using System;
using System.Runtime.Serialization;
using fiskaltrust.ifPOS.v1;

namespace fiskaltrust.Middleware.SCU.ME.FiscalizationService.Helpers
{
    [Serializable]
    public class FiscalizationException : ScuException
    {
        public int ErrorCode { get; set; }
        public string Operation { get; set; }

        public FiscalizationException() { }

        public FiscalizationException(string message) : base(message) { }

        public FiscalizationException(string message, int errorCode, string operation) : base(message)
        {
            ErrorCode = errorCode;
            Operation = operation;
        }

        public FiscalizationException(string message, Exception innerException) : base(message, innerException) { }

        protected FiscalizationException(SerializationInfo serializationInfo, StreamingContext streamingContext) : base(serializationInfo, streamingContext) { }

    }
}
