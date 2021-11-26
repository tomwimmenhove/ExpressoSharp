/* This file is part of Expresso
 *
 * Copyright (c) 2021 Tom Wimmenhove. All rights reserved.
 * Licensed under the MIT license. See LICENSE file in the project root for details.
 */

namespace ExpressoSharp
{
    public class CompilerException : ExpressoException
    {
        public CompilerException(string message)
             : base(message)
        { }
    }
}
