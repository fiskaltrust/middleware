using System;
using System.Runtime.Serialization;
using fiskaltrust.ifPOS.v1;

namespace fiskaltrust.Middleware.SCU.DE.FiskalyCertified.Exceptions
{
    [Serializable]
    public class FiskalyException : ScuException
    {
        public int ErrorCode { get; set; }
        public string Operation { get; set; }

        public FiskalyException() { }

        public FiskalyException(string message) : base(message) { }

        public FiskalyException(string message, int errorCode, string operation) : base(message)
        {
            ErrorCode = errorCode;
            Operation = operation;
        }

        public FiskalyException(string message, Exception innerException) : base(message, innerException) { }

        protected FiskalyException(SerializationInfo serializationInfo, StreamingContext streamingContext) : base(serializationInfo, streamingContext) { }
    }
}
