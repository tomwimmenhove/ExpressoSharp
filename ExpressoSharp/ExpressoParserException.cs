/* This file is part of Expresso
 *
 * Copyright (c) 2021 Tom Wimmenhove. All rights reserved.
 * Licensed under the MIT license. See LICENSE file in the project root for details.
 */

namespace ExpressoSharp
{
    /// <summary>
    /// An ExpressoParserException is thrown whenever parsing of an expression fails
    /// </summary>
    public class ExpressoParserException : ExpressoException
    {
        /// <summary>
        /// Create an instance of ExpressoParserException
        /// </summary>
        /// <param name="message">The exception message</param>
        public ExpressoParserException(string message)
             : base(message)
        { }
    }
}
