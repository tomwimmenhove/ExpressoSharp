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
        public static T CompileExpression<T>(string expression, params string[] parameterNames) where T : Delegate
        {
            var method = ExpressoMethod.CreateNamedMethod<T>("SingleMethod", expression, parameterNames);
            var assembly = Compile("SingleNameSpace", "SingleClass", method);

            return (T) DelegateFromMethod(assembly.GetType("SingleNameSpace.SingleClass"), method);
        }

        public static Delegate[] CompileExpressions(params ExpressoMethod[] methods)
        {
            var assembly = Compile("SingleNameSpace", "SingleClass", methods);
            
            return methods.Select(x => DelegateFromMethod(assembly.GetType("SingleNameSpace.SingleClass"), x)).ToArray();
        }

        private static Delegate DelegateFromMethod(Type type, ExpressoMethod method)
        {
            var parameterTypes = method.Parameters.Select(x => x.Type).ToArray();
            var methodInfo = type.GetMethod(method.Name, 0, parameterTypes);

            return Delegate.CreateDelegate(method.DelegateType, null, methodInfo);
        }

        private static Assembly Compile(string namespaceName, string className,
            params ExpressoMethod[] methods)
        {
            var allTypes = new HashSet<Type>(methods
                .SelectMany(x => x.Parameters.Select(x => x.Type))
                .Concat(methods.Select(x => x.ReturnType))
                .Append(typeof(object)));

            var compilationUnit = CreateCompilationUnitSyntax(
                namespaceName, className, methods);

            //System.Diagnostics.Debug.WriteLine(compilationUnit.NormalizeWhitespace().ToString());

            return Compile(compilationUnit.SyntaxTree, allTypes);
        }

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

        private static CompilationUnitSyntax CreateCompilationUnitSyntax(string nameSpaceName, string className,
            params ExpressoMethod[] methods) =>
            SyntaxFactory.CompilationUnit().AddMembers(
                SyntaxFactory.NamespaceDeclaration(SyntaxFactory.IdentifierName(nameSpaceName)).AddMembers(
                    SyntaxFactory.ClassDeclaration(className).AddMembers(
                        methods.Select(x => x.ToMethodDeclarationSyntax()).ToArray())));
    }
}
