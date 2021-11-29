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
    public class ExpressoFieldOptions : IExpressoVariableOptions, IExpressoSecurityOptions, IExpressoRewriteyOptions
    {
        /// <summary>
        /// This varible is of the dynamic type
        /// </summary>
        public bool IsDynamic { get; set; } = false;

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
