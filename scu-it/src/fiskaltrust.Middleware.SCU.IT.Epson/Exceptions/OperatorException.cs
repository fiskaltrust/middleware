using System;
using System.Runtime.Serialization;

namespace fiskaltrust.Middleware.SCU.IT.Epson.Exceptions
{
    [Serializable]
    public class OperatorException : ArgumentException
    {
        public static readonly string _message = "CbUser {0} not valid. The operator from cbuser field has to be an integer in the range of 1 to 12.";

        public OperatorException() { }
        public OperatorException(string cbuser) : base(string.Format(_message, cbuser)) { }
        public OperatorException(string cbuser, Exception inner) : base(string.Format(_message, cbuser), inner) { }
        protected OperatorException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }
}
