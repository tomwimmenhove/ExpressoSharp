/* This file is part of Expresso
 *
 * Copyright (c) 2021 Tom Wimmenhove. All rights reserved.
 * Licensed under the MIT license. See LICENSE file in the project root for details.
 */

using System;

namespace ExpressoSharp
{
    /// <summary>
    /// The base Exception class for all ExpressoSharp exceptions
    /// </summary>
    public class ExpressoException : Exception
    {
        /// <summary>
        /// Create an instance of ExpressoSecurityException
        /// </summary>
        /// <param name="message">The exception message</param>
        public ExpressoException(string message)
             : base(message)
        { }
    }
}
