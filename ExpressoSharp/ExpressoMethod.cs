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
    public class ExpressoMethod<T> : IExpressoMethod where T : Delegate
    {
        public string Name => _name;
        public string Expression { get; }
        public IReadOnlyCollection<ExpressoParameter> Parameters { get; }
        public Type ReturnType { get; }
        public ExpressoMethodOptions Options { get; }

        Type IExpressoMethod.DelegateType => typeof(T);
        MethodDeclarationSyntax IExpressoMethod.SyntaxNode => _syntaxNode;

        private string _name = UniqueName();
        private MethodDeclarationSyntax _syntaxNode;

        public ExpressoMethod(string expression, ExpressoParameter[] parameters)
             : this(new ExpressoMethodOptions(), expression, parameters, null, parameters.Length)
        { }

        public ExpressoMethod(string expression, params string[] parameterNames)
             : this(new ExpressoMethodOptions(), expression, null, parameterNames, parameterNames.Length)
        { }

        public ExpressoMethod(ExpressoMethodOptions options, string expression, ExpressoParameter[] parameters)
             : this(options, expression, parameters, null, parameters.Length)
        { }

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
