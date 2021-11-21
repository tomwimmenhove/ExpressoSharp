using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
//using Microsoft.CodeAnalysis.VisualBasic;


namespace ACO
{
    class MainForm
    {
        public void Main()
        {
        }
    }
}



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
            var bla = syntaxTree.ToString();

            Console.WriteLine(bla);

            return Compile(syntaxTree);
        }

        public static Assembly Compile(SyntaxTree syntaxTree)
        {
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
            //var statement = SyntaxFactory.ParseStatement("return 42;");
            var expression = SyntaxFactory.ParseExpression("42");

            var comp = SyntaxFactory.CompilationUnit()
                .AddMembers(
                    SyntaxFactory.NamespaceDeclaration(SyntaxFactory.IdentifierName("InMemory"))
                        .AddMembers(
                        SyntaxFactory.ClassDeclaration("CalcClass")
                            .AddMembers(
                                // SyntaxFactory.MethodDeclaration(SyntaxFactory.PredefinedType(
                                //     SyntaxFactory.Token(SyntaxKind.VoidKeyword)), "Main")
                                SyntaxFactory.MethodDeclaration(SyntaxFactory.PredefinedType(
                                    SyntaxFactory.Token(SyntaxKind.DoubleKeyword)), "Calc")
                                //SyntaxFactory.MethodDeclaration(SyntaxFactory.ParseTypeName("void"), "Main")
                                    .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword))
                                    //.WithBody(SyntaxFactory.Block(statement))
                                    .WithBody(SyntaxFactory.Block(SyntaxFactory.ReturnStatement(expression)))
                            )
                    )
            );

            var bla = comp.NormalizeWhitespace().ToString();

            // namespaceACO{classMainForm{System.Windows.Forms.TimerTicker{get;set;}publicvoidMain(){}}}

            Console.WriteLine(bla);

            Assembly assembly;
            try
            {
                assembly = Compiler.Compile(comp.SyntaxTree);
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

            var calc = DelegateConverter<Func<double>>(assembly, "InMemory.CalcClass", "Calc");

            Console.WriteLine(calc());

            return;
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

            //Assembly assembly;
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
