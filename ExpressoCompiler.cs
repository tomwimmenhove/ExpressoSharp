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
            var parameterTypes = method.Parameters.Select(x => x.Type).ToArray();
            var singleMethod = assembly.GetType("SingleNameSpace.SingleClass")
                .GetMethod("SingleMethod", 0, parameterTypes);

            return (T) Delegate.CreateDelegate(typeof(T), null, singleMethod);
        }

        public static Delegate[] CompileExpressions(params ExpressoMethod[] methods)
        {
            var assembly = Compile("SingleNameSpace", "SingleClass", methods);
            
            var delegates = new Delegate[methods.Length];
            for (var i = 0; i < methods.Length; i++)
            {
                var method = methods[i];
                var parameterTypes = method.Parameters.Select(x => x.Type).ToArray();
                var methodInfo = assembly.GetType("SingleNameSpace.SingleClass")
                    .GetMethod(methods[i].Name, 0, parameterTypes);

                delegates[i] = Delegate.CreateDelegate(method.DelegateType, null, methodInfo);                
            }

            return delegates;
        }

        private static Assembly Compile(string namespaceName, string className,
            params ExpressoMethod[] methods)
        {
            var allTypes = new HashSet<Type>();
            foreach (var method in methods)
            {
                foreach(var parameter in method.Parameters)
                {
                    allTypes.Add(parameter.Type);
                }

                if (method.ReturnType != typeof(void))
                {
                    allTypes.Add(method.ReturnType);
                }
            }
            allTypes.Add(typeof(object));

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
