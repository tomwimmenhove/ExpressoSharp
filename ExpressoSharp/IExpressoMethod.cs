using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ExpressoSharp
{
    public interface IExpressoMethod
    {
        string Name { get; }
        IReadOnlyCollection<ExpressoParameter> Parameters { get; }
        Type ReturnType { get; }
        bool ReturnsDynamic { get; }

        Type DelegateType { get; }
        MethodDeclarationSyntax SyntaxNode { get; }
    }
}
