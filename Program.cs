using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.VisualBasic;

namespace Expresso
{
    public class CompilerException : Exception
    {
        public IEnumerable<Diagnostic> Diagnostics { get; }

        public CompilerException(string message, IEnumerable<Diagnostic> diagnostics)
             : base(message)
        {
            Diagnostics = diagnostics;
        }
    }

    public class Compiler
    {
        public static Assembly Compile(string code)
        {
            var syntaxTree = CSharpSyntaxTree.ParseText(code);
            //var syntaxTree = VisualBasicSyntaxTree.ParseText(code);

            var references = new MetadataReference[]
            {
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                //MetadataReference.CreateFromFile(typeof(Enumerable).Assembly.Location)
            };

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

    class Program
    {
        public static T DelegateConverter<T>(Assembly assembly, string typeName, string memberName) where T: Delegate
        {
            var member = assembly.GetType(typeName).GetMember(memberName);

            return (T) Delegate.CreateDelegate(typeof(T), null, (MethodInfo)member[0]);
        }



        static void Main(string[] args)
        {
            var code =
            @"
                using System;

                namespace RoslynCompileSample
                {
                    public class TestClass
                    {
                        public static int Compute(int x)
                        {
                            return x * 2;
                        }
                    }
                }
            ";

            Assembly assembly;
            try
            {
                assembly = Compiler.Compile(code);
            }
            catch (CompilerException e)
            {
                var failures = e.Diagnostics.Where(diagnostic =>
                    diagnostic.IsWarningAsError || diagnostic.Severity == DiagnosticSeverity.Error);

                foreach (Diagnostic diagnostic in failures)
                {
                    Console.Error.WriteLine("{0}: {1}", diagnostic.Id, diagnostic.GetMessage());
                }

                return;
            }

            var f = DelegateConverter<Func<int, int>>(assembly, "RoslynCompileSample.TestClass", "Compute");

            Console.WriteLine(f(21));
        }
    }
}
