/* This file is part of Expresso
 *
 * Copyright (c) 2021 Tom Wimmenhove. All rights reserved.
 * Licensed under the MIT license. See LICENSE file in the project root for details.
 */

using System;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ExpressoSharp
{
    public interface IExpressoMethod
    {
        string Name { get; }
        ExpressoParameter[] Parameters { get; }
        Type ReturnType { get; }
        bool ReturnsDynamic { get; }

        Type DelegateType { get; }
        MethodDeclarationSyntax SyntaxNode { get; }
    }

    public class ExpressoMethod<T> : IExpressoMethod where T : Delegate
    {
        public string Name => _name;
        public string Expression { get; }
        public ExpressoParameter[] Parameters { get; }
        public Type ReturnType { get; }
        public bool ReturnsDynamic { get; }

        Type IExpressoMethod.DelegateType => _delegateType;
        MethodDeclarationSyntax IExpressoMethod.SyntaxNode => _syntaxNode;

        private string _name = UniqueName();
        private Type _delegateType;
        private MethodDeclarationSyntax _syntaxNode;

        public ExpressoMethod(string expression, params string[] parameterNames)
             : this(expression, false, parameterNames)
        { }

        public ExpressoMethod(string expression, bool objectsAsDynamic,
            params string[] parameterNames)
        {
            /* Use reflection to determine how T (which is a delegate) is to be invoked */
            var invokeMethod = typeof(T).GetMethod("Invoke");
            var parameters = invokeMethod.GetParameters();
            if (parameters.Count() != parameterNames.Count())
            {
                throw new ArgumentException($"Number of parameter names ({parameters.Count()}) does not match the numbers of parameters of the delegate type ({parameterNames.Count()})");
            }

            Expression = expression;
            ReturnType = invokeMethod.ReturnType;
            ReturnsDynamic = objectsAsDynamic && invokeMethod.ReturnType == typeof(object);
            _delegateType = typeof(T);

            /* Use this information to create the ExpressoParameter list with the correct types */
            Parameters = new ExpressoParameter[parameters.Count()];
            for (var i = 0; i < parameters.Count(); i++)
            {
                var parameterType = parameters[i].ParameterType;

                Parameters[i] = new ExpressoParameter(parameterNames[i], parameterType,
                    objectsAsDynamic && parameterType == typeof(object));
            }

            /* Parse the expression that is to be compiled */
            var parsedExpression = SyntaxFactory.ParseExpression(Expression);
            var errors = parsedExpression.GetDiagnostics()
                .Where(x => x.IsWarningAsError || x.Severity == DiagnosticSeverity.Error);
            if (errors.Any())
            {
                throw new ParserException(string.Join("\n", errors.Select(x => x.GetMessage())));
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
                var typeSyntax = ReturnsDynamic
                    ? SyntaxFactory.ParseTypeName("dynamic")
                    : SyntaxFactory.ParseTypeName(ReturnType.FullName);

                _syntaxNode = SyntaxFactory.MethodDeclaration(typeSyntax, Name).AddModifiers(
                    SyntaxFactory.Token(SyntaxKind.PublicKeyword)).AddParameterListParameters(
                        Parameters.Select(x => x.SyntaxNode).ToArray())
                    .WithBody(SyntaxFactory.Block(SyntaxFactory.ReturnStatement(parsedExpression)));
            }
        }

        private static string UniqueName() => $"_{Guid.NewGuid().ToString("N")}";
    }
}
