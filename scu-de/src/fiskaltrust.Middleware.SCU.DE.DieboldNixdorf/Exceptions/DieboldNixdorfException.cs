using System;
using System.Runtime.Serialization;
using fiskaltrust.ifPOS.v1;

namespace fiskaltrust.Middleware.SCU.DE.DieboldNixdorf.Exceptions
{
    [Serializable]
    public class DieboldNixdorfException : ScuException
    {
        public DieboldNixdorfException() { }

        public DieboldNixdorfException(string message) : base(message) { }

        public DieboldNixdorfException(string message, Exception innerException) : base(message, innerException) { }

        protected DieboldNixdorfException(SerializationInfo serializationInfo, StreamingContext streamingContext) : base(serializationInfo, streamingContext) { }
    }
}
