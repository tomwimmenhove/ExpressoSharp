/* This file is part of Expresso
 *
 * Copyright (c) 2021 Tom Wimmenhove. All rights reserved.
 * Licensed under the MIT license. See LICENSE file in the project root for details.
 */

namespace ExpressoSharp
{
    public class ExpressoFieldOptions : IExpressoVariableOptions, IExpressoSecurityOptions, IExpressoRewriteyOptions
    {
        public bool IsDynamic { get; set; } = false;
        public eExpressoSecurityAccess ExpressoSecurityAccess { get; set; } = eExpressoSecurityAccess.AllowAll;
        public bool ForceNumericDouble { get; set; } = false;
    }
}
