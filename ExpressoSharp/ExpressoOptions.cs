/* This file is part of Expresso
 *
 * Copyright (c) 2021 Tom Wimmenhove. All rights reserved.
 * Licensed under the MIT license. See LICENSE file in the project root for details.
 */

using System;

namespace ExpressoSharp
{
    [Flags]
    public enum eExpressoSecurityAccess : int
    {
        None = 0x00,
        AllowMathMethods = 0x01,
        AllowMemberAccess = 0x02,
        AllowMemberInvokation = 0x04,
        AllowAll = 0x7fffffff
    }

    public interface IExpressoVariableOptions
    {
        bool IsDynamic { get; }
    }

    public interface IExpressoSecurityOptions
    {
        eExpressoSecurityAccess ExpressoSecurityAccess { get; }
    }

    public interface IExpressoRewriteyOptions
    {
        bool ForceNumericDouble { get; }
    }

    public class ExpressoPropertyOptions : IExpressoVariableOptions
    {
        public bool IsDynamic { get; set; } = false;
    }

    public class ExpressoFieldOptions : IExpressoVariableOptions, IExpressoSecurityOptions, IExpressoRewriteyOptions
    {
        public bool IsDynamic { get; set; } = false;
        public eExpressoSecurityAccess ExpressoSecurityAccess { get; set; } = eExpressoSecurityAccess.AllowAll;
        public bool ForceNumericDouble { get; set; } = false;
    }

    public class ExpressoParameterOptions : IExpressoVariableOptions
    {
        public bool IsDynamic { get; set; } = false;
    }

    public class ExpressoMethodOptions : IExpressoSecurityOptions, IExpressoRewriteyOptions
    {
        public bool ReturnsDynamic { get; set; }
        public eExpressoSecurityAccess ExpressoSecurityAccess { get; set; } = eExpressoSecurityAccess.AllowAll;
        public bool ForceNumericDouble { get; set; } = false;

        public ExpressoParameterOptions DefaultParameterOptions { get; set; } = new ExpressoParameterOptions();
    }
}
