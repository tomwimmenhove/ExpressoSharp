using System;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Expresso
{
    public class ExpressoMethod
    {
        internal MethodDeclarationSyntax MethodDeclarationSyntax { get; }

        public Type DelegateType { get; }
        public string Name { get; }
        public Type ReturnType { get; }
        public string Expression { get;}
        public ExpressoParameter[] Parameters { get; }

        private ExpressoMethod(Type delegateType, string name, Type returnType, string expression,
            params ExpressoParameter[] parameters)
        {
            DelegateType = delegateType;
            Name = name;
            ReturnType = returnType;
            Expression = expression;
            Parameters = parameters;
        }

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

        internal MethodDeclarationSyntax ToMethodDeclarationSyntax()
        {
            var returnStatement = SyntaxFactory.ParseExpression(Expression);
            var expressionDiagnostics = returnStatement.GetDiagnostics();

            if (expressionDiagnostics.Any())
            {
                throw new CompilerException("Compilation failed", expressionDiagnostics);
            }

            return SyntaxFactory.MethodDeclaration(SyntaxFactory.ParseTypeName(ReturnType.FullName), Name).AddModifiers(
                SyntaxFactory.Token(SyntaxKind.PublicKeyword)).AddParameterListParameters(
                    Parameters.Select(x => x.ToParameterSyntax()).ToArray())
                .WithBody(SyntaxFactory.Block(SyntaxFactory.ReturnStatement(returnStatement)));
        }
    }
}
