/* This file is part of Expresso
 *
 * Copyright (c) 2021 Tom Wimmenhove. All rights reserved.
 * Licensed under the MIT license. See LICENSE file in the project root for details.
 */

using System;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ExpressoSharp
{
    internal class ExpressoRewriter : CSharpSyntaxRewriter
    {
        private IExpressoRewriteyOptions _options;

        public static SyntaxNode Rewrite(IExpressoRewriteyOptions options, SyntaxNode node)
        {
            if (!options.ForceNumericDouble)
            {
                return node;
            }

            var rewriter = new ExpressoRewriter(options);
            return rewriter.Visit(node);
        }
        
        public ExpressoRewriter(IExpressoRewriteyOptions options)
        {
            _options = options;
        }

        public override SyntaxNode VisitLiteralExpression(LiteralExpressionSyntax node)
        {
            if (_options.ForceNumericDouble &&
                node.Kind() == SyntaxKind.NumericLiteralExpression &&
                node.Token.Value.GetType() != typeof(double))
            {
                return node.Update(SyntaxFactory.Literal(Convert.ToDouble(node.Token.Value)));
            }

            return node;
        }
       
    }
}
