/* This file is part of Expresso
 *
 * Copyright (c) 2021 Tom Wimmenhove. All rights reserved.
 * Licensed under the MIT license. See LICENSE file in the project root for details.
 */
 
namespace ExpressoSharp
{
    public class ExpressoMethodOptions : IExpressoSecurityOptions, IExpressoRewriteyOptions
    {
        public bool ReturnsDynamic { get; set; }
        public eExpressoSecurityAccess ExpressoSecurityAccess { get; set; } = eExpressoSecurityAccess.AllowAll;
        public bool ForceNumericDouble { get; set; } = false;

        public ExpressoParameterOptions DefaultParameterOptions { get; set; } = new ExpressoParameterOptions();
    }

}
