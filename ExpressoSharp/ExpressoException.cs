/* This file is part of Expresso
 *
 * Copyright (c) 2021 Tom Wimmenhove. All rights reserved.
 * Licensed under the MIT license. See LICENSE file in the project root for details.
 */

using System;

namespace ExpressoSharp
{
    public class ExpressoException : Exception
    {
        public ExpressoException(string message)
             : base(message)
        { }
    }
}