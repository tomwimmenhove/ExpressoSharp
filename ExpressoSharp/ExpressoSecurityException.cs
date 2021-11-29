/* This file is part of Expresso
 *
 * Copyright (c) 2021 Tom Wimmenhove. All rights reserved.
 * Licensed under the MIT license. See LICENSE file in the project root for details.
 */

namespace ExpressoSharp
{
    /// <summary>
    /// An ExpressoSecurityException is thrown whenever an expression violates the set security options
    /// </summary>
    public class ExpressoSecurityException : ExpressoParserException
    {
        /// <summary>
        /// Create an instance of ExpressoSecurityException
        /// </summary>
        /// <param name="message">The exception message</param>
        public ExpressoSecurityException(string message)
             : base(message)
        { }
    }
}
