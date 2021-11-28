/* This file is part of Expresso
 *
 * Copyright (c) 2021 Tom Wimmenhove. All rights reserved.
 * Licensed under the MIT license. See LICENSE file in the project root for details.
 */

using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ExpressoSharp
{
    public static class ExpressoCompiler
    {
        public static T CompileExpression<T>(string expression,
            ICollection<IExpressoVariable> variables, params string[] parameterNames) where T : Delegate =>
            CompileExpression<T>(expression, variables, false, parameterNames);

        public static T CompileExpression<T>(string expression,
            ICollection<IExpressoVariable> variables, bool objectsAsDynamic, params string[] parameterNames) where T : Delegate
        {
            var method = new ExpressoMethod<T>(expression, objectsAsDynamic, parameterNames);
            var assembly = Compile("ExpressoSharp", "ExpressoClass", variables, method);
            var assemblyType = assembly.GetType("ExpressoSharp.ExpressoClass");

            InitializeVariables(assemblyType, variables);

            return (T) DelegateFromMethod(assemblyType, method);
        }

        public static T CompileExpression<T>(string expression, bool objectsAsDynamic,
            params string[] parameterNames) where T : Delegate =>
            CompileExpression<T>(expression, new IExpressoVariable[0], objectsAsDynamic, parameterNames);            

        public static T CompileExpression<T>(string expression,
            params string[] parameterNames) where T : Delegate =>
            CompileExpression<T>(expression, new IExpressoVariable[0], false, parameterNames);            

        public static Delegate[] CompileExpressions(ICollection<IExpressoVariable> variables, params IExpressoMethod[] methods)
        {
            var assembly = Compile("ExpressoSharp", "ExpressoClass", variables, methods);
            var assemblyType = assembly.GetType("ExpressoSharp.ExpressoClass");

            InitializeVariables(assemblyType, variables);
            
            return methods.Select(x => DelegateFromMethod(assemblyType, x)).ToArray();
        }

        public static Delegate[] CompileExpressions(params IExpressoMethod[] methods) =>
            CompileExpressions(new IExpressoVariable[0], methods);

        /* Compile a dummy program to force all needed assemblies to be loaded */
        // XXX: trade-off: slower Prime() for initializing dynamic type as well?
        public static void Prime() => CompileExpression<Func<object>>("null");

        private static Delegate DelegateFromMethod(Type type, IExpressoMethod method)
        {
            /* Find a method with the correct name and parameter signature in the
             * given type and convert the returned MethodInfo to a delegate */
            var parameterTypes = method.Parameters.Select(x => x.Type).ToArray();
            var methodInfo = type.GetMethod(method.Name, parameterTypes);

            return Delegate.CreateDelegate(method.DelegateType, null, methodInfo);
        }

        private static void InitializeVariables(Type type, ICollection<IExpressoVariable> variables)
        {
            foreach(var variable in variables)
            {
                variable.PostCompilation(type);
            }
        }

        private static Assembly Compile(string namespaceName, string className,
            ICollection<IExpressoVariable> variables, params IExpressoMethod[] methods)
        {
            /* Create a unique list of all assemblies needed for
             * any return, parameter and variable type used */
            var usedAssemblies = new HashSet<string>(methods
                .SelectMany(x => x.Parameters.Select(y => y.Type))
                .Concat(methods.Select(x => x.ReturnType))
                .Concat(variables.Select(x => x.Type))
                .Append(typeof(object))
                .Select(x => x.Assembly.Location))
                .ToList();

            /* Add additional assemblies in case the dynamic type is used */
            if (methods.Any(x => x.ReturnsDynamic || x.Parameters.Any(y => y.IsDynamic)) ||
                variables.Any(x => x.IsDynamic))
            {
                usedAssemblies.Add(typeof(System.Runtime.CompilerServices.DynamicAttribute).Assembly.Location);
                usedAssemblies.Add(typeof(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo).Assembly.Location);

                /* When compiling in .NET5, using the dynamic type will throw a
                 * "Missing compiler required member 'Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create'"
                 * error, unless the "System.Runtime" assembly is manually referenced.
                 * This is a work-around for that issues. */
                try
                {
                    usedAssemblies.Add(Assembly.Load(new AssemblyName("System.Runtime")).Location);
                }
                catch(FileNotFoundException)
                {
                    /* Whatever... */
                }
            }

            var compilationUnit = CreateCompilationUnitSyntax(namespaceName, className, variables, methods);

            //System.Diagnostics.Debug.WriteLine(compilationUnit.NormalizeWhitespace().ToString());

            return Compile(compilationUnit.SyntaxTree, usedAssemblies);
        }

        private static CompilationUnitSyntax CreateCompilationUnitSyntax(string nameSpaceName, string className,
            ICollection<IExpressoVariable> variables, IExpressoMethod[] methods) =>
            SyntaxFactory.CompilationUnit().AddUsings(
                SyntaxFactory.UsingDirective(SyntaxFactory.Token(SyntaxKind.StaticKeyword),
                    null, SyntaxFactory.ParseName("System.Math")))
                .AddMembers(SyntaxFactory.NamespaceDeclaration(SyntaxFactory.IdentifierName(nameSpaceName)).AddMembers(
                    SyntaxFactory.ClassDeclaration(className).AddMembers(
                        variables.SelectMany(x => x.SyntaxNodes)
                            .Concat(methods.Select(x => (MemberDeclarationSyntax)x.SyntaxNode)).ToArray()
                    )));

        private static Assembly Compile(SyntaxTree syntaxTree, IEnumerable<string> usedAssemblies)
        {
            var references = usedAssemblies.Select(x => MetadataReference.CreateFromFile(x));

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
                    var message = string.Join("\n",
                        result.Diagnostics.Where(x => x.IsWarningAsError || x.Severity == DiagnosticSeverity.Error)
                            .Select(x => x.GetMessage()));
                    throw new CompilerException(message);
                }

                ms.Seek(0, SeekOrigin.Begin);

                return Assembly.Load(ms.ToArray());
            }
        }
    }
}
