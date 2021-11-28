using System;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ExpressoSharp
{
    public class ExpressoRewriter : CSharpSyntaxRewriter
    {
        public override SyntaxNode VisitLiteralExpression(LiteralExpressionSyntax node)
        {
            if (node.Kind() == SyntaxKind.NumericLiteralExpression &&
               node.Token.Value.GetType() != typeof(double))
            {
                return node.Update(SyntaxFactory.Literal(Convert.ToDouble(node.Token.Value)));
            }

            return node;
        }
       
    }
}
