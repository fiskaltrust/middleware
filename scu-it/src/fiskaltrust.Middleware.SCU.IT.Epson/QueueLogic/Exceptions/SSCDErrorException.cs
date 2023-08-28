using System;
using System.Runtime.Serialization;
using fiskaltrust.ifPOS.v1.errors;

namespace fiskaltrust.Middleware.SCU.IT.Epson.QueueLogic.Exceptions
{
    [Serializable]
    public class SSCDErrorException : Exception
    {
        public SSCDErrorType Type { get; private set; }

        public SSCDErrorException(SSCDErrorType type) { Type = type; }

        public SSCDErrorException(SSCDErrorType type, string message) : base(message) { Type = type; }
        public SSCDErrorException(SSCDErrorType type, string message, Exception inner) : base(message, inner) { Type = type; }
        protected SSCDErrorException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }
}
