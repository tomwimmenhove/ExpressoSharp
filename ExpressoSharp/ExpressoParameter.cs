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
    /// <summary>
    /// The ExpressoParameter represents a parameter passed to a compile function that can be used in/by an expression
    /// </summary>
    public class ExpressoParameter
    {
        /// <summary>
        /// The name (as it will be used in expressions) of this parameter
        /// </summary>
        /// <value></value>
        public string Name { get; }

        /// <summary>
        /// The type of this parameter
        /// </summary>
        public Type Type { get; }

        /// <summary>
        /// Options to alter the behavior of the compiler
        /// </summary>
        public ExpressoParameterOptions Options { get; }

        internal ParameterSyntax SyntaxNode { get; }

        /// <summary>
        /// Create an instance of ExpressoParameter
        /// </summary>
        /// <param name="name">The name (as it will be used in expressions) of this parameter</param>
        /// <param name="type">The type of this parameter</param>
        public ExpressoParameter(string name, Type type)
            : this(new ExpressoParameterOptions(), name, type)
        { }

        /// <summary>
        /// Create an instance of ExpressoParameter
        /// </summary>
        /// <param name="options">Options to alter the behavior of the compiler</param>
        /// <param name="name">The name (as it will be used in expressions) of this parameter</param>
        /// <param name="type">The type of this parameter</param>
        public ExpressoParameter(ExpressoParameterOptions options, string name, Type type)
        {
            if (options.IsDynamic && type != typeof(object))
            {
                throw new ArgumentException($"The {nameof(type)} parameter must be {typeof(object)} when the {nameof(options.IsDynamic)} option is set to true");
            }

            Name = name;
            Type = type;
            Options = options;

            var typeSyntax = options.IsDynamic
                ? SyntaxFactory.ParseTypeName("dynamic")
                : SyntaxFactory.ParseTypeName(type.FullName);

            SyntaxNode = SyntaxFactory.Parameter(SyntaxFactory.Identifier(name)).WithType(typeSyntax);
        }
    }
}
