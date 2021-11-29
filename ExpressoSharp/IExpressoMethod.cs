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
    /// Interface used to implement ExpressoMethod&lt;T&gt; instances
    /// </summary>
    public interface IExpressoMethod
    {
        /// <summary>
        /// The name of this method
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Parameters passed to the compiled function
        /// </summary>
        IReadOnlyCollection<ExpressoParameter> Parameters { get; }

        /// <summary>
        /// The return type of the compiled function
        /// </summary>
        Type ReturnType { get; }

        /// <summary>
        /// Options to alter the behavior of the compiler
        /// </summary>
        ExpressoMethodOptions Options { get; }

        /// <summary>
        /// The type of this delegate (I.E. Func&lt;..., int&gt; or Action&lt;...&gt;)
        /// </summary>
        /// <value></value>
        Type DelegateType { get; }

        /// <summary>
        /// SyntaxNodes used by the Roslyn compiler
        /// </summary>
        MethodDeclarationSyntax SyntaxNode { get; }
    }
}
