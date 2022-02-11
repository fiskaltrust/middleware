using System;
using System.Runtime.Serialization;

namespace fiskaltrust.Middleware.SCU.DE.Epson.Exceptions
{
    [Serializable]
    public class NoResponseException : Exception
    {
        public NoResponseException() { }

        public NoResponseException(string message) : base(message) { }

        public NoResponseException(string message, Exception innerException) : base(message, innerException) { }

        protected NoResponseException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }
}