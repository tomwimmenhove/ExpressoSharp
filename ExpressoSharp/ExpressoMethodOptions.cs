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
    public class ExpressoMethodOptions : IExpressoSecurityOptions, IExpressoRewriteyOptions
    {
        /// <summary>
        /// Set to true if this function's return type is dynamic
        /// </summary>
        public bool ReturnsDynamic { get; set; }

        /// <summary>
        /// The default options passed to ExpressoParameter's constructor when creating ExpressoMethod instances
        /// </summary>
        public ExpressoParameterOptions DefaultParameterOptions { get; set; } = new ExpressoParameterOptions();

        /// <summary>
        /// Set what is allowed to be used inside of an expression
        /// </summary>
        public eExpressoSecurityAccess ExpressoSecurityAccess { get; set; } = eExpressoSecurityAccess.AllowAll;

        /// <summary>
        /// When this is set, any non-double numberic literals will automatically be replaced by doubles.
        /// </summary>
        public bool ForceNumericDouble { get; set; } = false;
    }
}
