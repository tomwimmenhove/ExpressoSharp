/* This file is part of Expresso
 *
 * Copyright (c) 2021 Tom Wimmenhove. All rights reserved.
 * Licensed under the MIT license. See LICENSE file in the project root for details.
 */

namespace ExpressoSharp
{
    /// <summary>
    /// An ExpressoCompilerException is thrown whenever the Roslyn compiler detects errors
    /// </summary>
    public class ExpressoCompilerException : ExpressoException
    {
        /// <summary>
        /// Create an instance of ExpressoCompilerException
        /// </summary>
        /// <param name="message">The exception message</param>
        public ExpressoCompilerException(string message)
             : base(message)
        { }
    }
}
