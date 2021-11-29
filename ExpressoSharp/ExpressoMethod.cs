/* This file is part of Expresso
 *
 * Copyright (c) 2021 Tom Wimmenhove. All rights reserved.
 * Licensed under the MIT license. See LICENSE file in the project root for details.
 */

using System;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ExpressoSharp
{
    /// <summary>
    /// This class represents an expression's method
    /// </summary>
    /// <typeparam name="T">The delegate type that will represent this method (I.E. Func&lt;..., int&gt; or Action&lt;...&gt;)</typeparam>
    public class ExpressoMethod<T> : IExpressoMethod where T : Delegate
    {
        /// <summary>
        /// The name of this method
        /// </summary>
        public string Name => _name;

        /// <summary>
        /// Parameters passed to the compiled function
        /// </summary>
        public IReadOnlyCollection<ExpressoParameter> Parameters { get; }

        /// <summary>
        /// The return type of the compiled function
        /// </summary>
        public Type ReturnType { get; }

        /// <summary>
        /// Options to alter the behavior of the compiler
        /// </summary>
        public ExpressoMethodOptions Options { get; }

        /// <summary>
        /// The epxression that will be used to compile this method
        /// </summary>
        /// <value></value>
        public string Expression { get; }

        Type IExpressoMethod.DelegateType => typeof(T);
        MethodDeclarationSyntax IExpressoMethod.SyntaxNode => _syntaxNode;

        private string _name = UniqueName();
        private MethodDeclarationSyntax _syntaxNode;

        /// <summary>
        /// Create an instance of ExpressoMethod
        /// </summary>
        /// <param name="expression"></param>
        /// <param name="parameters">An array of ExpressoParameter containing information about parameters passed to the compiled function</param>
        public ExpressoMethod(string expression, ExpressoParameter[] parameters)
             : this(new ExpressoMethodOptions(), expression, parameters, null, parameters.Length)
        { }

        /// <summary>
        /// Create an instance of ExpressoMethod
        /// </summary>
        /// <param name="expression">A string containing the expression to be compiled</param>
        /// <param name="parameterNames">Names of parameters used in the function</param>
        public ExpressoMethod(string expression, params string[] parameterNames)
             : this(new ExpressoMethodOptions(), expression, null, parameterNames, parameterNames.Length)
        { }

        /// <summary>
        /// Create an instance of ExpressoMethod
        /// </summary>
        /// <param name="options">Options to alter the behavior of the compiler</param>
        /// <param name="expression">A string containing the expression to be compiled</param>
        /// <param name="parameters">An array of ExpressoParameter containing information about parameters passed to the compiled function</param>
        public ExpressoMethod(ExpressoMethodOptions options, string expression, ExpressoParameter[] parameters)
             : this(options, expression, parameters, null, parameters.Length)
        { }

        /// <summary>
        /// Create an instance of ExpressoMethod
        /// </summary>
        /// <param name="options">Options to alter the behavior of the compiler</param>
        /// <param name="expression">A string containing the expression to be compiled</param>
        /// <param name="parameterNames">Names of parameters used in the function</param>
        public ExpressoMethod(ExpressoMethodOptions options, string expression, params string[] parameterNames)
             : this(options, expression, null, parameterNames, parameterNames.Length)
        { }

        private ExpressoMethod(ExpressoMethodOptions options, string expression,
            ExpressoParameter[] parameters, string[] parameterNames, int numParameters)
        {
            /* Use reflection to determine how T (which is a delegate) is to be invoked */
            var invokeMethod = typeof(T).GetMethod("Invoke");
            var invokeParameters = invokeMethod.GetParameters();
            if (invokeParameters.Length != numParameters)
            {
                throw new ArgumentException($"Number of parameter names ({invokeParameters.Count()}) does not match the numbers of parameters of the delegate type ({parameterNames.Count()})");
            }

            Options = options;
            Expression = expression;
            ReturnType = invokeMethod.ReturnType;

            /* Parse the expression that is to be compiled */
            var parsedExpression = SyntaxFactory.ParseExpression(expression);
            var errors = parsedExpression.GetDiagnostics()
                .Where(x => x.IsWarningAsError || x.Severity == DiagnosticSeverity.Error);
            if (errors.Any())
            {
                throw new ExpressoParserException(string.Join("\n", errors.Select(x => x.GetMessage())));
            }

            parsedExpression = (ExpressionSyntax) ExpressoRewriter.Rewrite(options, parsedExpression);
            ExpressoSecurity.Check(options, parsedExpression);

            if (parameters != null)
            {
                Parameters = parameters;
            }
            else
            {
                Parameters = CreateParameters(options, parameterNames, invokeParameters);
            }

            /* The return type of our method (void or not void)
             * changes the way the SyntaxNode is constructed */
            if (ReturnType == typeof(void))
            {
                _syntaxNode = SyntaxFactory.MethodDeclaration(SyntaxFactory.PredefinedType(
                    SyntaxFactory.Token(SyntaxKind.VoidKeyword)), Name).AddModifiers(
                        SyntaxFactory.Token(SyntaxKind.PublicKeyword)).AddParameterListParameters(
                            Parameters.Select(x => x.SyntaxNode).ToArray())
                        .WithBody(SyntaxFactory.Block(SyntaxFactory.ExpressionStatement(parsedExpression)));
            }
            else
            {
                var typeSyntax = options.ReturnsDynamic
                    ? SyntaxFactory.ParseTypeName("dynamic")
                    : SyntaxFactory.ParseTypeName(ReturnType.FullName);

                _syntaxNode = SyntaxFactory.MethodDeclaration(typeSyntax, Name).AddModifiers(
                    SyntaxFactory.Token(SyntaxKind.PublicKeyword)).AddParameterListParameters(
                        Parameters.Select(x => x.SyntaxNode).ToArray())
                    .WithBody(SyntaxFactory.Block(SyntaxFactory.ReturnStatement(parsedExpression)));
            }
        }

        private static ExpressoParameter[] CreateParameters(ExpressoMethodOptions options,
            string[] parameterNames, ParameterInfo[] parameterInfo)
        {
            var expressoParameters = new ExpressoParameter[parameterInfo.Length];
            for (var i = 0; i < parameterInfo.Length; i++)
            {
                var parameterType = parameterInfo[i].ParameterType;

                expressoParameters[i] = new ExpressoParameter(options.DefaultParameterOptions,
                    parameterNames[i], parameterType);
            }

            return expressoParameters;
        }

        private static string UniqueName() => $"_{Guid.NewGuid().ToString("N")}";
    }
}
