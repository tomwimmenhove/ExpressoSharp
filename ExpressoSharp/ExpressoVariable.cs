/* This file is part of Expresso
 *
 * Copyright (c) 2021 Tom Wimmenhove. All rights reserved.
 * Licensed under the MIT license. See LICENSE file in the project root for details.
 */

using System;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ExpressoSharp
{
    public abstract class ExpressoVariable
    {
        public string Name { get; protected set; }
        public Type Type { get; protected set; }
        public bool IsDynamic { get; protected set; }

        internal MemberDeclarationSyntax[] SyntaxNodes { get; set; }

        internal abstract void Init(Type type);
    }
}
