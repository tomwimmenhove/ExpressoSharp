/* This file is part of Expresso
 *
 * Copyright (c) 2021 Tom Wimmenhove. All rights reserved.
 * Licensed under the MIT license. See LICENSE file in the project root for details.
 */

using System;

namespace ExpressoSharp
{
    [Flags]
    public enum eExpressoSecurityAccess : int
    {
        None = 0x00,
        AllowMathMethods = 0x01,
        AllowMemberAccess = 0x02,
        AllowMemberInvokation = 0x04,
        AllowAll = 0x7fffffff
    }
}
