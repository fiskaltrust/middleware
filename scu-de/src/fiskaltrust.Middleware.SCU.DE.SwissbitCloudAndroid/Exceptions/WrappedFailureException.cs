using DE.Fiskal.Connector.Android.Api;
using fiskaltrust.ifPOS.v1;
using System;
using System.Runtime.Serialization;

namespace fiskaltrust.Middleware.SCU.DE.SwissbitCloudAndroid.Exceptions
{
    [Serializable]
    public class WrappedFailureException : ScuException
    {
        public Failure Failure { get; set; }

        public WrappedFailureException(Failure failure) => Failure = failure;

        public WrappedFailureException() { }

        public WrappedFailureException(string message) : base(message) { }

        public WrappedFailureException(string message, Exception innerException) : base(message, innerException) { }

        protected WrappedFailureException(SerializationInfo serializationInfo, StreamingContext streamingContext) : base(serializationInfo, streamingContext) { }
    }
}