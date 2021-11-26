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
    public class ExpressoParameter
    {
        public string Name { get; }
        public Type Type { get; }
        public bool IsDynamic { get; }

        internal ParameterSyntax SyntaxNode { get; }

        public ExpressoParameter(string name, Type type, bool isDynamic = false)
        {
            if (isDynamic && type != typeof(object))
            {
                throw new ArgumentException($"The {nameof(type)} parameter must be {typeof(object)} when {nameof(isDynamic)} is set to true");
            }

            Name = name;
            Type = type;
            IsDynamic = isDynamic;

            var typeSyntax = isDynamic
                ? SyntaxFactory.ParseTypeName("dynamic")
                : SyntaxFactory.ParseTypeName(type.FullName);

            SyntaxNode = SyntaxFactory.Parameter(SyntaxFactory.Identifier(name)).WithType(typeSyntax);
        }
    }
}
