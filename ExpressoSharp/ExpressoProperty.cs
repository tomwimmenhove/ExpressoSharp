/* This file is part of Expresso
 *
 * Copyright (c) 2021 Tom Wimmenhove. All rights reserved.
 * Licensed under the MIT license. See LICENSE file in the project root for details.
 */

using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ExpressoSharp
{
    /// <summary>
    /// The ExpressoProperty class represents a property that can be used by expressions.
    /// The value of an ExpressoProperty will be shared between multiple compilation units. This means that, if 
    /// multiple functions using the same ExpressoProperty are compiled by separate calls to ExpressoCompiler's
    /// compile methods, assigning a new value in one function will reflect on the value of this property in every
    /// function.
    /// </summary>
    /// <typeparam name="T">The type of the property</typeparam>
    public class ExpressoProperty<T> : IExpressoVariable
    {
        /// <summary>
        /// The name (as it will be used in expressions) of this property
        /// </summary>
        /// <value></value>
        public string Name { get; }

        /// <summary>
        /// The type of this property
        /// </summary>
        public Type Type { get; }

        /// <summary>
        /// Options to alter the behavior of the compiler
        /// </summary>
        public IExpressoVariableOptions Options { get; }

        /// <summary>
        /// The value of this property
        /// </summary>
        public T Value { get; set; }

        IReadOnlyCollection<MemberDeclarationSyntax> IExpressoVariable.SyntaxNodes => _syntaxNodes;

        private string _getterName { get; }
        private string _setterName { get; }
        private MemberDeclarationSyntax[] _syntaxNodes;

        /// <summary>
        /// Create an ExpressoProperty instance
        /// </summary>
        /// <param name="name">The name (as it will be used in expressions) of this property</param>
        /// <param name="value">An initial value to assign to this property</param>
        public ExpressoProperty(string name, T value = default)
            : this(new ExpressoPropertyOptions(), name, value)
        { }

        /// <summary>
        /// Create an ExpressoProperty instance
        /// </summary>
        /// <param name="options">Options to alter the behavior of the compiler</param>
        /// <param name="name">The name (as it will be used in expressions) of this property</param>
        /// <param name="value">An initial value to assign to this property</param>
        public ExpressoProperty(ExpressoPropertyOptions options, string name, T value = default)
        {
            var type = typeof(T);
            if (options.IsDynamic && type != typeof(object))
            {
                throw new ArgumentException($"The {nameof(type)} parameter must be {typeof(object)} when the {nameof(options.IsDynamic)} option is set to true");
            }

            Value = value;
            Name = name;
            Type = type;
            Options = options;

            /* A unique name for the getter */
            var unique = $"{name}_{Guid.NewGuid().ToString("N")}";
            _getterName = $"_get_{unique}";
            _setterName = $"_set_{unique}";

            var variableDeclaration = SyntaxFactory.VariableDeclarator(name);

            /* Since the dynamic type is not a real type, it has to be set explicitely */
            var typeName = options.IsDynamic ? "dynamic" : type.FullName;
            var typeSyntax = SyntaxFactory.ParseTypeName(typeName);

            /* This is the property that the compiled expression will use as it's 'variable'.
             * It will call a getter and setter provided by us behind the scenes */
            var propertySyntaxNode = SyntaxFactory.PropertyDeclaration(typeSyntax, SyntaxFactory.Identifier(Name))
                .AddModifiers(SyntaxFactory.Token(SyntaxKind.PrivateKeyword),
                    SyntaxFactory.Token(SyntaxKind.StaticKeyword))
                .AddAccessorListAccessors( 
                    /* Add a getter */
                    SyntaxFactory.AccessorDeclaration(SyntaxKind.GetAccessorDeclaration,
                        SyntaxFactory.Block(
                            SyntaxFactory.ReturnStatement(
                                SyntaxFactory.InvocationExpression(SyntaxFactory.IdentifierName(_getterName))
                            )
                        )
                    ),
                    /* And a setter */
                    SyntaxFactory.AccessorDeclaration(SyntaxKind.SetAccessorDeclaration,
                        SyntaxFactory.Block(
                            SyntaxFactory.ExpressionStatement(
                                SyntaxFactory.InvocationExpression(SyntaxFactory.IdentifierName(_setterName))
                                .AddArgumentListArguments(SyntaxFactory.Argument(SyntaxFactory.IdentifierName("value")))
                            )
                        )
                    )
                );

            /* These are the getter and setter fields that will be set
             * in the IExpressoVariable.PostCompilation() method */
            var getterSyntaxNode = SyntaxFactory.FieldDeclaration(SyntaxFactory.VariableDeclaration(
                SyntaxFactory.ParseTypeName($"System.Func<{typeName}>"))
                    .AddVariables(SyntaxFactory.VariableDeclarator(_getterName)))
                    .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword), SyntaxFactory.Token(SyntaxKind.StaticKeyword));

            var setterSyntaxNode = SyntaxFactory.FieldDeclaration(SyntaxFactory.VariableDeclaration(
                SyntaxFactory.ParseTypeName($"System.Action<{typeName}>"))
                    .AddVariables(SyntaxFactory.VariableDeclarator(_setterName)))
                    .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword), SyntaxFactory.Token(SyntaxKind.StaticKeyword));

            _syntaxNodes = new MemberDeclarationSyntax[]
                { getterSyntaxNode, setterSyntaxNode, propertySyntaxNode, };
        }

        void IExpressoVariable.PostCompilation(Type type)
        {
            /* Set the getter and setter delegates of the compiled type
             * the getter and setter of this variable's Value property */
            var valueProperty = GetType().GetProperty(nameof(Value));
            var valueGetter = Delegate.CreateDelegate(typeof(Func<T>),   this, valueProperty.GetGetMethod());
            var valueSetter = Delegate.CreateDelegate(typeof(Action<T>), this, valueProperty.GetSetMethod());

            type.GetField(_getterName).SetValue(null, valueGetter);;
            type.GetField(_setterName).SetValue(null, valueSetter);
        }
    }
}
