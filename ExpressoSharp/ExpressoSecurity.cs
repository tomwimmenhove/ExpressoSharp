using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ExpressoSharp
{
    internal class ExpressoSecurity : CSharpSyntaxWalker
    {
        public override void VisitInvocationExpression(InvocationExpressionSyntax node)
        {
            var okNames = new HashSet<string>(typeof(Math).GetMethods().Concat(typeof(object).GetMethods()).Select(x => x.Name));

            if (node.Expression is IdentifierNameSyntax id && !okNames.Contains(id.Identifier.ValueText))
            {
                throw new SecurityException($"The name '{id.Identifier.ValueText}' does not exist in the current context");
            }

            foreach (var child in node.ChildNodes())
            {
                Visit(child);
            }
        }
    }
}
