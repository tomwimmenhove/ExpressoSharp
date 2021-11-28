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
        private const string _errorFormat = "The name '{0}' does not exist in the current context";

        private IExpressoSecurityOptions _options;
        private HashSet<string> _allowedMethods = new HashSet<string>();

        public static void Check(IExpressoSecurityOptions options, SyntaxNode node)
        {
            if (!options.ExpressoSecurityAccess.HasFlag(eExpressoSecurityAccess.AllowAll))
            {
                var security = new ExpressoSecurity(options);
                security.Visit(node);
            }
        }

        public ExpressoSecurity(IExpressoSecurityOptions options)
        {
            _options = options;

            if (options.ExpressoSecurityAccess == 0)
            {
                return;
            }

            if (options.ExpressoSecurityAccess.HasFlag(eExpressoSecurityAccess.AllowMathMethods))
            {
                _allowedMethods = new HashSet<string>(typeof(Math).GetMethods().Select(x => x.Name));
            }
        }

        public override void VisitMemberAccessExpression(MemberAccessExpressionSyntax node)
        {
            if (!_options.ExpressoSecurityAccess.HasFlag(eExpressoSecurityAccess.AllowMemberAccess))
            {
                throw new SecurityException(string.Format(_errorFormat, node.Name));
            }
        }

        public override void VisitInvocationExpression(InvocationExpressionSyntax node)
        {
            if (node.Expression is IdentifierNameSyntax id && !_allowedMethods.Contains(id.Identifier.ValueText))
            {
                throw new SecurityException(string.Format(_errorFormat, id.Identifier.ValueText));
            }

            if (node.Expression is MemberAccessExpressionSyntax ma &&
                !_options.ExpressoSecurityAccess.HasFlag(eExpressoSecurityAccess.AllowMemberInvokation))
            {
                throw new SecurityException(string.Format(_errorFormat, ma.Name));
            }

            foreach (var child in node.ChildNodes())
            {
                Visit(child);
            }
        }
    }
}
