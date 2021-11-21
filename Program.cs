using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
//using Microsoft.CodeAnalysis.VisualBasic;

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

    internal static class TypeExtensions
    {
        internal static TypeSyntax ToTypeSyntax(this Type type) =>
            type == typeof(void)
                ? SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.VoidKeyword))
                : SyntaxFactory.ParseTypeName(type.FullName);
    }

    public class ExpressoParameter
    {
        public string Name { get; }
        public Type Type { get; }

        public ExpressoParameter(string name, Type type)
        {
            Name = name;
            Type = type;
        }

        internal ParameterSyntax ToParameterSyntax() =>
            SyntaxFactory.Parameter(SyntaxFactory.Identifier(Name))
                .WithType(SyntaxFactory.ParseTypeName(Type.FullName));
    }

    public class ExpressoMethod
    {
        public string Name { get; }
        public Type ReturnType { get; }
        public string Expression { get;}
        public ExpressoParameter[] Parameters { get; }

        public ExpressoMethod(string name, Type returnType, string expression,
            params ExpressoParameter[] parameters)
        {
            Name = name;
            ReturnType = returnType;
            Expression = expression;
            Parameters = parameters;
        }

        internal MethodDeclarationSyntax ToMethodDeclarationSyntax()
        {
            var returnStatement = SyntaxFactory.ParseExpression(Expression);
            var expressionDiagnostics = returnStatement.GetDiagnostics();

            if (expressionDiagnostics.Any())
            {
                throw new CompilerException("Compilation failed", expressionDiagnostics);
            }

            return SyntaxFactory.MethodDeclaration(ReturnType.ToTypeSyntax(), Name).AddModifiers(
                SyntaxFactory.Token(SyntaxKind.PublicKeyword)).AddParameterListParameters(
                    Parameters.Select(x => x.ToParameterSyntax()).ToArray())
                .WithBody(SyntaxFactory.Block(SyntaxFactory.ReturnStatement(returnStatement)));
        }
    }

    public class ExpressionCompiler
    {
        public static void Test()
        {
            var calc = CompileExpression<Func<int, double>>("x * 42", "x");

            Console.WriteLine(calc(2));
        }

        public static T CompileExpression<T>(string expression, params string[] parameterNames) where T : Delegate
        {
            var method = CreateMethodDeclarationSyntax<T>("SingleMethod", expression, parameterNames);
            var compilationUnit = CreateCompilationUnitSyntax("SingleNameSpace", "SingleClass", method);
            var assembly = Compiler.Compile(compilationUnit.SyntaxTree);

            var member = assembly.GetType("SingleNameSpace.SingleClass").GetMember("SingleMethod");

            return (T) Delegate.CreateDelegate(typeof(T), null, (MethodInfo)member[0]);
        }

        private static MethodDeclarationSyntax CreateMethodDeclarationSyntax<T>(string name, string expression, params string[] parameterNames) where T : Delegate
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

            var method = new ExpressoMethod(name, invokeMethod.ReturnType, expression, expressoParameters);

            return method.ToMethodDeclarationSyntax();
        }

        private static T DelegateConverter<T>(Assembly assembly, string typeName, string memberName) where T : Delegate
        {
            var member = assembly.GetType(typeName).GetMember(memberName);

            return (T) Delegate.CreateDelegate(typeof(T), null, (MethodInfo)member[0]);
        }

        private static MethodDeclarationSyntax CreateMethodDeclarationSyntax(
            string name, Type returnType, string expression,
            params ExpressoParameter[] parameters)
        {
            var returnStatement = SyntaxFactory.ParseExpression(expression);
            var expressionDiagnostics = returnStatement.GetDiagnostics();

            if (expressionDiagnostics.Any())
            {
                throw new CompilerException("Compilation failed", expressionDiagnostics);
            }

            return SyntaxFactory.MethodDeclaration(returnType.ToTypeSyntax(), name).AddModifiers(
                SyntaxFactory.Token(SyntaxKind.PublicKeyword)).AddParameterListParameters(
                    parameters.Select(x => x.ToParameterSyntax()).ToArray())
                .WithBody(SyntaxFactory.Block(SyntaxFactory.ReturnStatement(returnStatement)));
        }

        private static CompilationUnitSyntax CreateCompilationUnitSyntax(string nameSpaceName, string className,
            params MethodDeclarationSyntax[] methods) =>
            SyntaxFactory.CompilationUnit().AddMembers
            (
                SyntaxFactory.NamespaceDeclaration(SyntaxFactory.IdentifierName(nameSpaceName)).AddMembers
                (
                    SyntaxFactory.ClassDeclaration(className).AddMembers(methods)
                )
            );
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
            ExpressionCompiler.Test();
            return;
            //var statement = SyntaxFactory.ParseStatement("return 42;");
            var expression = SyntaxFactory.ParseExpression("a * 21");

            var dia = expression.GetDiagnostics();

            var comp = SyntaxFactory.CompilationUnit().AddMembers
                (
                    SyntaxFactory.NamespaceDeclaration(SyntaxFactory.IdentifierName("InMemory")).AddMembers
                    (
                        SyntaxFactory.ClassDeclaration("CalcClass").AddMembers
                        (
                            SyntaxFactory.MethodDeclaration(SyntaxFactory.PredefinedType(
                                SyntaxFactory.Token(SyntaxKind.DoubleKeyword)), "Calc").AddModifiers
                            (
                                SyntaxFactory.Token(SyntaxKind.PublicKeyword)).AddParameterListParameters
                                (
                                    SyntaxFactory.Parameter(SyntaxFactory.Identifier("a"))
                                        .WithType(SyntaxFactory.ParseTypeName(typeof (int).FullName))
                                )
                                    .WithBody(SyntaxFactory.Block(SyntaxFactory.ReturnStatement(expression)))
                            )
                    )
            );
            
            var plaplapla = typeof(void);

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

            var calc = DelegateConverter<Func<int, double>>(assembly, "InMemory.CalcClass", "Calc");

            Console.WriteLine(calc(2));

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
