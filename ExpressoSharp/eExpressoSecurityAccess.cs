/* This file is part of Expresso
 *
 * Copyright (c) 2021 Tom Wimmenhove. All rights reserved.
 * Licensed under the MIT license. See LICENSE file in the project root for details.
 */

using System;

namespace ExpressoSharp
{
    /// <summary>
    /// Control what is allowed to be used inside of an expression
    /// </summary>
    [Flags]
    public enum eExpressoSecurityAccess : int
    {
        /// <summary>
        /// Only allows basic operations
        /// </summary>
        None = 0x00,

        /// <summary>
        /// Allow all static funcitons in the Math class
        /// </summary>
        AllowMathMethods = 0x01,

        /// <summary>
        /// Allow accessing object members (I.E. string.Length)
        /// </summary>
        AllowMemberAccess = 0x02,

        /// <summary>
        /// Allow invocation of object members (I.E. object.GetType())
        /// NOTE: This requires AllowMemberAccess to also be set.
        /// </summary>
        AllowMemberInvokation = 0x04,

        /// <summary>
        /// Sets all flags
        /// </summary>
        AllowAll = 0x7fffffff
    }
}
