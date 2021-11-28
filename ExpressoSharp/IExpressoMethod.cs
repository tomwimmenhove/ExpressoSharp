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
    public interface IExpressoMethod
    {
        string Name { get; }
        IReadOnlyCollection<ExpressoParameter> Parameters { get; }
        Type ReturnType { get; }
        ExpressoMethodOptions Options { get; }

        Type DelegateType { get; }
        MethodDeclarationSyntax SyntaxNode { get; }
    }
}
