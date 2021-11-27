/* This file is part of Expresso
 *
 * Copyright (c) 2021 Tom Wimmenhove. All rights reserved.
 * Licensed under the MIT license. See LICENSE file in the project root for details.
 */

using System;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ExpressoSharp
{
    public abstract class ExpressoVariable
    {
        public string Name { get; protected set; }
        public Type Type { get; protected set; }
        public bool IsDynamic { get; protected set; }

        internal MemberDeclarationSyntax[] SyntaxNodes { get; set; }

        internal abstract void Init(Type type);
    }

    public class ExpressoVariable<T> : ExpressoVariable
    {
        public T Value { get; set; }

        private string _getterName { get; }
        private string _setterName { get; }

        public ExpressoVariable(string name, T value = default)
            : this(false, name, value)
        { }

        public ExpressoVariable(bool isDynamic, string name, T value = default)
        {
            var type = typeof(T);
            if (isDynamic && type != typeof(object))
            {
                throw new ArgumentException($"The {nameof(type)} parameter must be {typeof(object)} when {nameof(isDynamic)} is set to true");
            }

            Value = value;
            Name = name;
            Type = type;
            IsDynamic = isDynamic;

            /* A unique name for the getter */
            _getterName = $"_getter_{name}_{Guid.NewGuid().ToString("N")}";
            _setterName = $"_setter_{name}_{Guid.NewGuid().ToString("N")}";

            var variableDeclaration = SyntaxFactory.VariableDeclarator(name);

            /* Since the dynamic type is not a real type, it has to be set explicitely */
            var typeName = isDynamic ? "dynamic" : type.FullName;
            var typeSyntax = SyntaxFactory.ParseTypeName(typeName);

            /* This is the property that the compiled expression will use as it's 'variable'.
             * It will call a getter and setter provided by us behind the scenes */
            var propertySyntaxNode = SyntaxFactory.PropertyDeclaration(typeSyntax, SyntaxFactory.Identifier(Name))
                .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword),
                    SyntaxFactory.Token(SyntaxKind.StaticKeyword))
                .AddAccessorListAccessors( 
                    /* Add a getter */
                    SyntaxFactory.AccessorDeclaration(
                        SyntaxKind.GetAccessorDeclaration,
                        SyntaxFactory.Block(
                            SyntaxFactory.ReturnStatement(
                                SyntaxFactory.InvocationExpression(SyntaxFactory.IdentifierName(_getterName))
                            )
                        )
                    ),
                    /* And a setter */
                    SyntaxFactory.AccessorDeclaration(
                        SyntaxKind.SetAccessorDeclaration,
                        SyntaxFactory.Block(
                            SyntaxFactory.ExpressionStatement(
                                SyntaxFactory.InvocationExpression(SyntaxFactory.IdentifierName(_setterName))
                                .AddArgumentListArguments(SyntaxFactory.Argument(SyntaxFactory.IdentifierName("value")))
                            )
                                
                        )
                    )
                );

            /* These are the getter and setter that will be set by us to 'attach' it to our local value */
            var getterSyntaxNode = SyntaxFactory.FieldDeclaration(SyntaxFactory.VariableDeclaration(
                SyntaxFactory.ParseTypeName($"System.Func<{typeName}>"))
                    .AddVariables(SyntaxFactory.VariableDeclarator(_getterName)))
                    .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword), SyntaxFactory.Token(SyntaxKind.StaticKeyword));

            var setterSyntaxNode = SyntaxFactory.FieldDeclaration(SyntaxFactory.VariableDeclaration(
                SyntaxFactory.ParseTypeName($"System.Action<{typeName}>"))
                    .AddVariables(SyntaxFactory.VariableDeclarator(_setterName)))
                    .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword), SyntaxFactory.Token(SyntaxKind.StaticKeyword));

            SyntaxNodes = new MemberDeclarationSyntax[] { getterSyntaxNode, setterSyntaxNode, propertySyntaxNode, };
        }

        internal override void Init(Type type)
        {
            /* Attach the getters and setters in the compiled class for this variable to
             * the getters and setters for this variable's Value property */
            Func<T> valueGetter = () => Value;
            Action<T> valueSetter = (value) => Value = value;

            type.GetField(_getterName).SetValue(null, valueGetter);;
            type.GetField(_setterName).SetValue(null, valueSetter);
        }
    }
}
