/* This file is part of Expresso
 *
 * Copyright (c) 2021 Tom Wimmenhove. All rights reserved.
 * Licensed under the MIT license. See LICENSE file in the project root for details.
 */

using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ExpressoSharp
{
    /// <summary>
    /// Interface for variables that can be used in expressions
    /// </summary>
    public interface IExpressoVariable
    {
        /// <summary>
        /// The name (as it will be used in expressions) of this variable
        /// </summary>
        string Name { get; }

        /// <summary>
        /// The type of this variable
        /// </summary>
        Type Type { get; }

        /// <summary>
        /// Options to alter the behavior of the compiler
        /// </summary>
        IExpressoVariableOptions Options { get; }

        /// <summary>
        /// SyntaxNodes used by the Roslyn compiler
        /// </summary>
        IReadOnlyCollection<MemberDeclarationSyntax> SyntaxNodes { get; }

        /// <summary>
        /// Called after compilation of an compilation unit
        /// </summary>
        /// <param name="type">The type of the compiled class</param>
        void PostCompilation(Type type);
    }
}
