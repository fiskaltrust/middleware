using System;
using System.Runtime.Serialization;
using fiskaltrust.ifPOS.v1;

namespace fiskaltrust.Middleware.SCU.DE.CryptoVision.Exceptions
{
    [Serializable]
    public class CryptoVisionException : ScuException
    {
        public Models.SeResult Error { get; set; }

        public CryptoVisionException() { }

        public CryptoVisionException(string message) : base(message) { }

        public CryptoVisionException(string message, Models.SeResult error) : base(message) => Error = error;

        public CryptoVisionException(string message, Exception innerException) : base(message, innerException) { }

        protected CryptoVisionException(SerializationInfo serializationInfo, StreamingContext streamingContext) : base(serializationInfo, streamingContext) { }
    }
}
