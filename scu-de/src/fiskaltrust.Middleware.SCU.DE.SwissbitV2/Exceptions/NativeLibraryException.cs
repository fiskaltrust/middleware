using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace fiskaltrust.Middleware.SCU.DE.SwissbitV2.Exceptions
{
    [Serializable]
    public class NativeLibraryException : Exception
    {
        public NativeLibraryException(string message) : base(message) { }

        public NativeLibraryException(string message, Exception innerException) : base(message, innerException) { }

        public NativeLibraryException() { }

        protected NativeLibraryException(SerializationInfo serializationInfo, StreamingContext streamingContext) : base(serializationInfo, streamingContext) { }
    }
}
