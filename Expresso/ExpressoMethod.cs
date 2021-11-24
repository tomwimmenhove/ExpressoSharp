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
        public bool ReturnsDynamic { get; }

        internal Type DelegateType { get; }
        internal MethodDeclarationSyntax SyntaxNode { get; }

        public static ExpressoMethod Create<T>(string expression, params string[] parameterNames) where T : Delegate =>
             CreateNamedMethod<T>($"_{Guid.NewGuid().ToString("N")}", expression, false, parameterNames);

        public static ExpressoMethod Create<T>(string expression, bool objectsAsDynamic,
            params string[] parameterNames) where T : Delegate =>
            CreateNamedMethod<T>($"_{Guid.NewGuid().ToString("N")}", expression, objectsAsDynamic, parameterNames);

        internal static ExpressoMethod CreateNamedMethod<T>(string name, string expression, bool objectsAsDynamic,
            params string[] parameterNames) where T : Delegate
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
                var parameterType = parameters[i].ParameterType;

                expressoParameters[i] = new ExpressoParameter(parameterNames[i], parameterType,
                    objectsAsDynamic && parameterType == typeof(object));
            }

            return new ExpressoMethod(typeof(T), name, invokeMethod.ReturnType,
                objectsAsDynamic && invokeMethod.ReturnType == typeof(object),
                expression, expressoParameters);
        }

        private ExpressoMethod(Type delegateType, string name, Type returnType, bool returnsDynamic,
            string expression, params ExpressoParameter[] parameters)
        {
            if (returnsDynamic && returnType != typeof(object))
            {
                throw new ArgumentException($"The {nameof(returnType)} parameter must be {typeof(object)} when {nameof(returnsDynamic)} is set to true");
            }

            DelegateType = delegateType;
            Name = name;
            ReturnType = returnType;
            Expression = expression;
            Parameters = parameters;
            ReturnsDynamic = returnsDynamic;

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
                    SyntaxFactory.Token(SyntaxKind.VoidKeyword)), Name).AddModifiers(
                        SyntaxFactory.Token(SyntaxKind.PublicKeyword)).AddParameterListParameters(
                            Parameters.Select(x => x.SyntaxNode).ToArray())
                        .WithBody(SyntaxFactory.Block(SyntaxFactory.ExpressionStatement(parsedExpression)));
            }
            else
            {
                var typeSyntax = returnsDynamic
                    ? SyntaxFactory.ParseTypeName("dynamic")
                    : SyntaxFactory.ParseTypeName(returnType.FullName);

                SyntaxNode = SyntaxFactory.MethodDeclaration(typeSyntax, Name).AddModifiers(
                    SyntaxFactory.Token(SyntaxKind.PublicKeyword)).AddParameterListParameters(
                        Parameters.Select(x => x.SyntaxNode).ToArray())
                    .WithBody(SyntaxFactory.Block(SyntaxFactory.ReturnStatement(parsedExpression)));
            }
        }
    }
}
