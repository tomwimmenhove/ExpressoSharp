using System;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Expresso
{
    public class ExpressoMethod
    {
        public string Name { get; }
        public string Expression { get; }
        public ExpressoParameter[] Parameters { get; }
        public Type ReturnType { get; }

        internal Type DelegateType { get; }
        internal MethodDeclarationSyntax SyntaxNode { get; }

        public static ExpressoMethod Create<T>(string expression, params string[] parameterNames) where T : Delegate =>
             CreateNamedMethod<T>($"_{Guid.NewGuid().ToString("N")}", expression, parameterNames);

        internal static ExpressoMethod CreateNamedMethod<T>(string name, string expression, params string[] parameterNames) where T : Delegate
        {
            var invokeMethod = typeof(T).GetMethod("Invoke");
            var parameters = invokeMethod.GetParameters();
            if (parameters.Count() != parameterNames.Count())
            {
                throw new ArgumentException($"Number of parameter names ({parameters.Count()}) does not match the numbers of parameters of the delegate type ({parameterNames.Count()})");
            }

            var expressoParameters = new ExpressoParameter[parameters.Count()];
            for (var i = 0; i < parameters.Count(); i++)
            {
                expressoParameters[i] = new ExpressoParameter(parameterNames[i], parameters[i].ParameterType);
            }

            return new ExpressoMethod(typeof(T), name, invokeMethod.ReturnType, expression, expressoParameters);
        }

        private ExpressoMethod(Type delegateType, string name, Type returnType, string expression,
            params ExpressoParameter[] parameters)
        {
            DelegateType = delegateType;
            Name = name;
            ReturnType = returnType;
            Expression = expression;
            Parameters = parameters;

            var parsedExpression = SyntaxFactory.ParseExpression(Expression);
            var errors = parsedExpression.GetDiagnostics()
                .Where(x => x.IsWarningAsError || x.Severity == DiagnosticSeverity.Error);

            if (errors.Any())
            {
                throw new ParserException(string.Join("\n", errors.Select(x => x.GetMessage())));
            }

            if (returnType == typeof(void))
            {
                SyntaxNode = SyntaxFactory.MethodDeclaration(SyntaxFactory.PredefinedType(
                    SyntaxFactory.Token(SyntaxKind.VoidKeyword)), Name)
                    .AddModifiers(
                        SyntaxFactory.Token(SyntaxKind.PublicKeyword)).AddParameterListParameters(
                            Parameters.Select(x => x.ToParameterSyntax()).ToArray())
                        .WithBody(SyntaxFactory.Block(SyntaxFactory.ExpressionStatement(parsedExpression)));
            }
            else
            {
                SyntaxNode = SyntaxFactory.MethodDeclaration(SyntaxFactory.ParseTypeName(ReturnType.FullName), Name)
                    .AddModifiers(
                        SyntaxFactory.Token(SyntaxKind.PublicKeyword)).AddParameterListParameters(
                            Parameters.Select(x => x.ToParameterSyntax()).ToArray())
                        .WithBody(SyntaxFactory.Block(SyntaxFactory.ReturnStatement(parsedExpression)));
            }
        }
    }
}
