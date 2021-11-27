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
    public class ExpressoField<T> : IExpressoVariable
    {
        public string Name { get; }
        public Type Type { get; }
        public bool IsDynamic { get; }

        IReadOnlyCollection<MemberDeclarationSyntax> IExpressoVariable.SyntaxNodes => _syntaxNodes;

        private string _getterName { get; }
        private string _setterName { get; }
        private MemberDeclarationSyntax[] _syntaxNodes;

        public ExpressoField(string name, string initializer = null)
            : this(false, name, initializer)
        { }

        public ExpressoField(bool isDynamic, string name, string initializer = null)
        {
            var type = typeof(T);
            if (isDynamic && type != typeof(object))
            {
                throw new ArgumentException($"The {nameof(type)} parameter must be {typeof(object)} when {nameof(isDynamic)} is set to true");
            }

            Name = name;
            Type = type;
            IsDynamic = isDynamic;

            /* Since the dynamic type is not a real type, it has to be set explicitely */
            var typeSyntax = isDynamic
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
                    throw new ParserException(string.Join("\n", errors.Select(x => x.GetMessage())));
                }

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
