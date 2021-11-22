using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace Expresso
{
    public class CompilerException : ExpressoException
    {
        public CompilerException(string message)
             : base(message)
        { }
    }
}
