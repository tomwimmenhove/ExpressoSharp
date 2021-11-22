using System;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Expresso
{
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
}
