using System;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Expresso
{
    public abstract class ExpressoVariable
    {
        public string Name { get; }
        public Type Type { get; }

        internal PropertyDeclarationSyntax SyntaxNode { get; }
        internal abstract void Init(PropertyInfo property);

        internal ExpressoVariable(string name, string initialValue, Type type)
        {
            Name = name;
            Type = type;

            SyntaxNode = SyntaxFactory.PropertyDeclaration(SyntaxFactory.ParseTypeName(Type.FullName), Name)
                .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword), SyntaxFactory.Token(SyntaxKind.StaticKeyword))
                .AddAccessorListAccessors(
                    SyntaxFactory.AccessorDeclaration(SyntaxKind.GetAccessorDeclaration)
                        .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken)),
                    SyntaxFactory.AccessorDeclaration(SyntaxKind.SetAccessorDeclaration)
                        .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken)));
                        
            if (initialValue!= null)
            {
                var initialExpression = SyntaxFactory.ParseExpression(initialValue);
                var errors = initialExpression.GetDiagnostics()
                    .Where(x => x.IsWarningAsError || x.Severity == DiagnosticSeverity.Error);

                if (errors.Any())
                {
                    throw new ParserException(string.Join("\n", errors.Select(x => x.GetMessage())));
                }

                SyntaxNode.WithInitializer(SyntaxFactory.EqualsValueClause(initialExpression))
                    .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken));
            }
        }
    }

    public class ExpressoVariable<T> : ExpressoVariable
    {
        public T Value
        {
            get => _getter();
            set => _setter(value);
        }

        private Func<T> _getter;
        private Action<T> _setter;

        public ExpressoVariable(string name, string initialValue = null)
            : base(name, initialValue, typeof(T))
        { }

        internal override void Init(PropertyInfo property)
        {
            _getter = (Func<T>) Delegate.CreateDelegate(typeof(Func<T>), property.GetGetMethod());
            _setter = (Action<T>) Delegate.CreateDelegate(typeof(Action<T>), property.GetSetMethod());
        }
    }
}
