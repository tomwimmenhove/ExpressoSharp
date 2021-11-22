using System;

namespace Expresso
{
    public class ExpressoException : Exception
    {
        public ExpressoException(string message)
             : base(message)
        { }
    }
}
