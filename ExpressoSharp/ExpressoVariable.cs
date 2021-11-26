/* This file is part of Expresso
 *
 * Copyright (c) 2021 Tom Wimmenhove. All rights reserved.
 * Licensed under the MIT license. See LICENSE file in the project root for details.
 */

using System;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ExpressoSharp
{
    public abstract class ExpressoVariable
    {
        public string Name { get; }
        public Type Type { get; }
        public bool IsDynamic { get; }

        protected string PropertyName { get; }

        internal PropertyDeclarationSyntax PropertySyntaxNode { get; }
        internal FieldDeclarationSyntax FieldSyntaxNode { get; }

        internal abstract void Init(Type type);

        public static ExpressoVariable<T> Create<T>(string name) =>
            new ExpressoVariable<T>(name, null, false);

        public static ExpressoVariable<T> Create<T>(string name, T initialValue) =>
            new ExpressoVariable<T>(name, initialValue, false, true);

        public static ExpressoVariable<T> CreateWithInitializer<T>(string name, string initializer) =>
            new ExpressoVariable<T>(name, initializer, false);

        public static ExpressoVariable<T> Create<T>(bool isDynamic, string name) =>
            new ExpressoVariable<T>(name, null, isDynamic);

        public static ExpressoVariable<T> Create<T>(bool isDynamic, string name, T initialValue) =>
            new ExpressoVariable<T>(name, initialValue, isDynamic, true);

        public static ExpressoVariable<T> CreateWithInitializer<T>(bool isDynamic, string name, string initializer) =>
            new ExpressoVariable<T>(name, initializer, isDynamic);

        internal ExpressoVariable(string name, string initialValue, Type type, bool isDynamic)
        {
            if (isDynamic && type != typeof(object))
            {
                throw new ArgumentException($"The {nameof(type)} parameter must be {typeof(object)} when {nameof(isDynamic)} is set to true");
            }

            Name = name;
            Type = type;
            IsDynamic = isDynamic;

            /* A unique name for the getter */
            PropertyName = $"Get_{name}_{Guid.NewGuid().ToString("N")}";

            var variableDeclaration = SyntaxFactory.VariableDeclarator(name);

            /* If an initial value expression is set, try to parse
             * it and add the initializer to the variableDeclaration */
            if (initialValue != null)
            {
                var initialExpression = SyntaxFactory.ParseExpression(initialValue);
                var errors = initialExpression.GetDiagnostics()
                    .Where(x => x.IsWarningAsError || x.Severity == DiagnosticSeverity.Error);
                if (errors.Any())
                {
                    throw new ParserException(string.Join("\n", errors.Select(x => x.GetMessage())));
                }

                variableDeclaration = variableDeclaration.WithInitializer(
                    SyntaxFactory.EqualsValueClause(initialExpression));
            }

            /* Since the dynamic type is not a real type, it has to be set explicitely */
            var typeSyntax = isDynamic
                ? SyntaxFactory.ParseTypeName("dynamic")
                : SyntaxFactory.ParseTypeName(type.FullName);

            /* This is the field that the compiled expression will be able to use */
            FieldSyntaxNode = SyntaxFactory.FieldDeclaration(SyntaxFactory.VariableDeclaration(typeSyntax)
                .AddVariables(variableDeclaration))
                .AddModifiers( SyntaxFactory.Token( SyntaxKind.PrivateKeyword), SyntaxFactory.Token(SyntaxKind.StaticKeyword));

            /* This is the property that we will 'attach' to when getting/setting the
             * value of this variable using ExpressoVariable<T>.Value */
            PropertySyntaxNode = SyntaxFactory.PropertyDeclaration(typeSyntax, SyntaxFactory.Identifier(PropertyName))
                .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword),
                    SyntaxFactory.Token(SyntaxKind.StaticKeyword))
                .AddAccessorListAccessors( 
                    /* Add a getter */
                    SyntaxFactory.AccessorDeclaration(
                        SyntaxKind.GetAccessorDeclaration,
                        SyntaxFactory.Block(
                            SyntaxFactory.ReturnStatement(SyntaxFactory.IdentifierName(Name))
                        )
                    ),
                    /* And a setter */
                    SyntaxFactory.AccessorDeclaration(
                        SyntaxKind.SetAccessorDeclaration,
                        SyntaxFactory.Block( 
                            SyntaxFactory.ExpressionStatement( 
                                SyntaxFactory.AssignmentExpression( 
                                    SyntaxKind.SimpleAssignmentExpression,
                                    SyntaxFactory.IdentifierName(Name),
                                    SyntaxFactory.IdentifierName("value")
                                )
                            )
                        )
                    )
                );
        }
    }

    public class ExpressoVariable<T> : ExpressoVariable
    {
        public T Value
        {
            get => _getter();
            set => _setter(value);
        }

        private Func<T> _getter = () => throw new NotSupportedException("This variable has not yet been initialized");
        private Action<T> _setter = (value) => throw new NotSupportedException("This variable has not yet been initialized");
        private T _initialValue;
        private bool _isInitialized;

        internal ExpressoVariable(string name, string initializer, bool isDynamic)
            : base(name, initializer, typeof(T), isDynamic)
        { }

        internal ExpressoVariable(string name, T initialValue, bool isDynamic, bool initialized)
            : base(name, null, typeof(T), isDynamic)
        {
            _initialValue = initialValue;

            _getter = () => _initialValue;
            _setter = (value) => _initialValue = value;

            _isInitialized = initialized;
        }

        internal override void Init(Type type)
        {
            /* The the getter and setter from the type in the compiled assembly and
             * attach it to the _getter and _setter members used by the Value property */
            var property = type.GetProperty(PropertyName);
            var getter = (Func<T>) Delegate.CreateDelegate(typeof(Func<T>), property.GetGetMethod());
            var setter = (Action<T>) Delegate.CreateDelegate(typeof(Action<T>), property.GetSetMethod());

            /* Copy the old value in case this variable has been used before */
            if (_isInitialized)
            {
                var oldValue = _getter();
                setter(oldValue);
            }

            _getter = getter;
            _setter = setter;

            _isInitialized = true;
        }
    }
}
