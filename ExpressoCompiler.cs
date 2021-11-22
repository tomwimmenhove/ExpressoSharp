using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Expresso
{
    public class ExpressoCompiler
    {
        public static T CompileExpression<T>(string expression,
            ExpressoVariable[] variables, params string[] parameterNames) where T : Delegate
        {
            var method = ExpressoMethod.CreateNamedMethod<T>("SingleMethod", expression, parameterNames);
            var assembly = Compile("SingleNameSpace", "SingleClass", variables, method);
            var assemblyType = assembly.GetType("SingleNameSpace.SingleClass");

            InitializeVariables(assemblyType, variables);

            return (T) DelegateFromMethod(assemblyType, method);
        }

        public static T CompileExpression<T>(string expression,
            params string[] parameterNames) where T : Delegate =>
            CompileExpression<T>(expression, new ExpressoVariable[0], parameterNames);            

        public static Delegate[] CompileExpressions(ExpressoVariable[] variables, params ExpressoMethod[] methods)
        {
            var assembly = Compile("SingleNameSpace", "SingleClass", variables, methods);
            var assemblyType = assembly.GetType("SingleNameSpace.SingleClass");

            InitializeVariables(assemblyType, variables);
            
            return methods.Select(x => DelegateFromMethod(assemblyType, x)).ToArray();
        }

        public static Delegate[] CompileExpressions(params ExpressoMethod[] methods) =>
            CompileExpressions(new ExpressoVariable[0], methods);

        private static Delegate DelegateFromMethod(Type type, ExpressoMethod method)
        {
            var parameterTypes = method.Parameters.Select(x => x.Type).ToArray();
            var methodInfo = type.GetMethod(method.Name, 0, parameterTypes);

            return Delegate.CreateDelegate(method.DelegateType, null, methodInfo);
        }

        private static void InitializeVariables(Type type, ExpressoVariable[] variables)
        {
            foreach(var variable in variables)
            {
                variable.Init(type.GetProperty(variable.Name));
            }
        }

        private static Assembly Compile(string namespaceName, string className,
            ExpressoVariable[] variables, params ExpressoMethod[] methods)
        {
            var allTypes = new HashSet<Type>(methods
                .SelectMany(x => x.Parameters.Select(x => x.Type))
                .Concat(methods.Select(x => x.ReturnType))
                .Concat(variables.Select(x => x.Type))
                .Append(typeof(object)));

            var compilationUnit = CreateCompilationUnitSyntax(
                namespaceName, className, variables, methods);

            System.Diagnostics.Debug.WriteLine(compilationUnit.NormalizeWhitespace().ToString());

            return Compile(compilationUnit.SyntaxTree, allTypes);
        }

        private static CompilationUnitSyntax CreateCompilationUnitSyntax(string nameSpaceName, string className,
            ExpressoVariable[] variables, ExpressoMethod[] methods) =>
            SyntaxFactory.CompilationUnit().AddUsings(
                SyntaxFactory.UsingDirective(SyntaxFactory.Token(SyntaxKind.StaticKeyword),
                    null, SyntaxFactory.ParseName("System.Math")))
                .AddMembers(SyntaxFactory.NamespaceDeclaration(SyntaxFactory.IdentifierName(nameSpaceName)).AddMembers(
                    SyntaxFactory.ClassDeclaration(className).AddMembers(
                        variables.Select(x => (MemberDeclarationSyntax)x.ToPropertyDeclarationSyntax())
                            .Concat(methods.Select(x => (MemberDeclarationSyntax)x.ToMethodDeclarationSyntax())).ToArray()
                    )));

        private static Assembly Compile(SyntaxTree syntaxTree, IEnumerable<Type> usedTypes)
        {
            var references = usedTypes.Select(x => MetadataReference.CreateFromFile(x.Assembly.Location));

            var compilation = CSharpCompilation.Create(
                "InMemoryAssembly",
                syntaxTrees: new[] { syntaxTree },
                references: references,
                options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

            using (var ms = new MemoryStream())
            {
                var result = compilation.Emit(ms);
                if (!result.Success)
                {
                    throw new CompilerException("Compilation failed", result.Diagnostics);
                }

                ms.Seek(0, SeekOrigin.Begin);

                return Assembly.Load(ms.ToArray());
            }
        }
    }
}
