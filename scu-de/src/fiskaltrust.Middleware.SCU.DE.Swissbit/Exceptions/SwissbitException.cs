using System;
using System.Runtime.Serialization;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.Middleware.SCU.DE.Swissbit.Interop;

namespace fiskaltrust.Middleware.SCU.DE.Swissbit.Exceptions
{
    [Serializable]
    public class SwissbitException : ScuException
    {
        public NativeFunctionPointer.WormError Error { get; set; }

        public SwissbitException(string message) : base(message) { }

        public SwissbitException(string message, NativeFunctionPointer.WormError error) : base(message) => Error = error;

        public SwissbitException(string message, Exception innerException) : base(message, innerException) { }

        public SwissbitException() { }

        protected SwissbitException(SerializationInfo serializationInfo, StreamingContext streamingContext) : base(serializationInfo, streamingContext) { }
    }
}
