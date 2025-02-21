using System;
using System.Runtime.Serialization;

namespace fiskaltrust.ifPOS.v1
{
    [Serializable]
    public class ScuException : Exception
    {
        public ScuException() { }

        public ScuException(string message) : base(message) { }

        public ScuException(string message, Exception innerException) : base(message, innerException) { }

        protected ScuException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }
}
