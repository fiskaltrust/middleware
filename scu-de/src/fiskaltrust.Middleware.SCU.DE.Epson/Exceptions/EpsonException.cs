using System;
using System.Runtime.Serialization;
using fiskaltrust.Middleware.SCU.DE.Epson.Models;

namespace fiskaltrust.Middleware.SCU.DE.Epson.Exceptions
{
    [Serializable]
    public class EpsonException : Exception
    {
        public EpsonError ErrorCode { get; }

        public EpsonException(string message) : base(message)
        {
        }

        public EpsonException(string message, EpsonError errorCode) : base(message)
        {
            ErrorCode = errorCode;
        }

        public EpsonException(string message, Exception innerException) : base(message, innerException)
        {
        }

        public EpsonException()
        {
        }

        protected EpsonException(SerializationInfo serializationInfo, StreamingContext streamingContext) : base(serializationInfo, streamingContext) { }
    }
}
