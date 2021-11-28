using System;

namespace ExpressoSharp
{
    public class SecurityException : ParserException
    {
        public SecurityException(string message)
             : base(message)
        { }
    }
}
