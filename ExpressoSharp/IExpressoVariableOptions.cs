/* This file is part of Expresso
 *
 * Copyright (c) 2021 Tom Wimmenhove. All rights reserved.
 * Licensed under the MIT license. See LICENSE file in the project root for details.
 */

namespace ExpressoSharp
{
    /// <summary>
    /// Options to alter the behavior of the compiler
    /// </summary>
    public interface IExpressoVariableOptions
    {
        /// <summary>
        /// This varible is of the dynamic type
        /// </summary>
        bool IsDynamic { get; }
    }
}
