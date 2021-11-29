/* This file is part of Expresso
 *
 * Copyright (c) 2021 Tom Wimmenhove. All rights reserved.
 * Licensed under the MIT license. See LICENSE file in the project root for details.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ExpressoSharp
{
    /// <summary>
    /// The ExpressoField class represents a field that can be used by expressions.
    /// The value of an ExpressoField is limited to one single compilation unit. This means that, if multiple
    /// functions using the same ExpressoField are compiled by separate calls to ExpressoCompiler's compile
    /// methods, assigning a value to this field in one function will have no effect on the value of this field
    /// in another function. However, multiple functions in the same compilation unit (I.E. compiled with
    /// ExpressoCompiler.CompileExpressions()) will share the same field, and assignments by one function will
    /// be 'seen' by all other functions within the same compilation unit.
    /// </summary>
    /// <typeparam name="T">The type of the field</typeparam>
    public class ExpressoField<T> : IExpressoVariable
    {
        /// <summary>
        /// The name (as it will be used in expressions) of this variable
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// The type of this variable
        /// </summary>
        public Type Type { get; }

        /// <summary>
        /// Options to alter the behavior of the compiler
        /// </summary>
        public IExpressoVariableOptions Options { get; }

        IReadOnlyCollection<MemberDeclarationSyntax> IExpressoVariable.SyntaxNodes => _syntaxNodes;

        private string _getterName { get; }
        private string _setterName { get; }
        private MemberDeclarationSyntax[] _syntaxNodes;

        /// <summary>
        /// Create an ExpressoField instance
        /// </summary>
        /// <param name="name">The name (as it will be used in expressions) of this field</param>
        /// <param name="initializer">A string representing the expression this field will be initialized with</param>
        public ExpressoField(string name, string initializer = null)
            : this(new ExpressoFieldOptions(), name, initializer)
        { }

        /// <summary>
        /// Create an ExpressoField instance
        /// </summary>
        /// <param name="options">Options to alter the behavior of the compiler</param>
        /// <param name="name">The name (as it will be used in expressions) of this field</param>
        /// <param name="initializer">A string representing the expression this field will be initialized with</param>
        public ExpressoField(ExpressoFieldOptions options, string name, string initializer = null)
        {
            var type = typeof(T);
            if (options.IsDynamic && type != typeof(object))
            {
                throw new ArgumentException($"The {nameof(type)} parameter must be {typeof(object)} when the {nameof(options.IsDynamic)} option is set to true");
            }

            Name = name;
            Type = type;
            Options = options;

            /* Since the dynamic type is not a real type, it has to be set explicitely */
            var typeSyntax = options.IsDynamic
                ? SyntaxFactory.ParseTypeName("dynamic")
                : SyntaxFactory.ParseTypeName(type.FullName);

            var variableDeclaration = SyntaxFactory.VariableDeclarator(name);

            /* If an initial value expression is set, try to parse
             * it and add the initializer to the variableDeclaration */
            if (initializer != null)
            {
                var initialExpression = SyntaxFactory.ParseExpression(initializer);
                var errors = initialExpression.GetDiagnostics()
                    .Where(x => x.IsWarningAsError || x.Severity == DiagnosticSeverity.Error);
                if (errors.Any())
                {
                    throw new ExpressoParserException(string.Join("\n", errors.Select(x => x.GetMessage())));
                }

                initialExpression = (ExpressionSyntax)ExpressoRewriter.Rewrite(options, initialExpression);
                ExpressoSecurity.Check(options, initialExpression);

                variableDeclaration = variableDeclaration.WithInitializer(
                    SyntaxFactory.EqualsValueClause(initialExpression));
            }

            /* This is the field that the compiled expression will be able to use */
            var fieldSyntaxNode = SyntaxFactory.FieldDeclaration(SyntaxFactory.VariableDeclaration(typeSyntax)
                .AddVariables(variableDeclaration))
                .AddModifiers(SyntaxFactory.Token(SyntaxKind.PrivateKeyword), SyntaxFactory.Token(SyntaxKind.StaticKeyword));

            _syntaxNodes = new MemberDeclarationSyntax[] { fieldSyntaxNode };
        }

        void IExpressoVariable.PostCompilation(Type type) { }
    }
}
