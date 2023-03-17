using System;
using System.Runtime.Serialization;

namespace fiskaltrust.Middleware.Contracts.Exceptions
{
    [Serializable]
    public class UnknownPayItemException : ArgumentException
    {
        private static readonly string _message = $"The given PayItemCase 0x{0:x} is not supported. Please refer to docs.fiskaltrust.cloud for supported cases.";
        public UnknownPayItemException() { }

        public UnknownPayItemException(long payItemCase) : base(string.Format(_message, payItemCase)) { }
        public UnknownPayItemException(string message, Exception inner) : base(message, inner) { }
        protected UnknownPayItemException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }
}
