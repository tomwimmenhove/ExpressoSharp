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

        MemberDeclarationSyntax[] IExpressoVariable.SyntaxNodes { get; set; }

        private string _getterName { get; }
        private string _setterName { get; }

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

            ((IExpressoVariable) this).SyntaxNodes = new MemberDeclarationSyntax[] { getterSyntaxNode, setterSyntaxNode, propertySyntaxNode, };
        }

        void IExpressoVariable.Init(Type type)
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
