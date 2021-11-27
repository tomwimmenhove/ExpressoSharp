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
    public class ExpressoProperty<T> : IExpressoVariable
    {
        public string Name { get; }
        public Type Type { get; }
        public bool IsDynamic { get; }

        public T Value { get; set; }

        MemberDeclarationSyntax[] IExpressoVariable.SyntaxNodes => _syntaxNodes;

        private string _getterName { get; }
        private string _setterName { get; }
        private MemberDeclarationSyntax[] _syntaxNodes;

        public ExpressoProperty(string name, T value = default)
            : this(false, name, value)
        { }

        public ExpressoProperty(bool isDynamic, string name, T value = default)
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
            var unique = $"{name}_{Guid.NewGuid().ToString("N")}";
            _getterName = $"_get_{unique}";
            _setterName = $"_set_{unique}";

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
