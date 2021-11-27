/* This file is part of Expresso
 *
 * Copyright (c) 2021 Tom Wimmenhove. All rights reserved.
 * Licensed under the MIT license. See LICENSE file in the project root for details.
 */

using System;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ExpressoSharp
{
    public interface IExpressoVariable
    {
        string Name { get; }
        Type Type { get; }
        bool IsDynamic { get; }

        MemberDeclarationSyntax[] SyntaxNodes { get; }

        void PostCompilation(Type type);
    }
}
