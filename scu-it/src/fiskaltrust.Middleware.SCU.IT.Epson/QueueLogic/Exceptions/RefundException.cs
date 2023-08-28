using System;
using System.Runtime.Serialization;

namespace fiskaltrust.Middleware.SCU.IT.Epson.QueueLogic.Exceptions
{
    [Serializable]
    public class RefundException : ArgumentException
    {
        public RefundException() { }
        public RefundException(string message) : base(message) { }
        public RefundException(string message, Exception inner) : base(message, inner) { }
        protected RefundException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }
}
