using DE.Fiskal.Connector.Android.Client.Library;
using fiskaltrust.ifPOS.v1;
using System;
using System.Runtime.Serialization;

namespace fiskaltrust.Middleware.SCU.DE.SwissbitCloudAndroid.Exceptions
{
    [Serializable]
    public class WrappedClientException : ScuException
    {
        public FccClientError FccClientError { get; set; }

        public WrappedClientException(FccClientError exc) => FccClientError = exc;

        public WrappedClientException() { }

        public WrappedClientException(string message) : base(message) { }

        public WrappedClientException(string message, Exception innerException) : base(message, innerException) { }

        protected WrappedClientException(SerializationInfo serializationInfo, StreamingContext streamingContext) : base(serializationInfo, streamingContext) { }
    }
}