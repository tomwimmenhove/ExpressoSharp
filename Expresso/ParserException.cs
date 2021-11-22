using System;

namespace Expresso
{
    public class ParserException : ExpressoException
    {
        public ParserException(string message)
             : base(message)
        { }
    }
}
