using System;
using System.Runtime.Serialization;
using fiskaltrust.ifPOS.v1;

namespace fiskaltrust.Middleware.SCU.DE.SwissbitCloudV2.Exceptions
{
    [Serializable]
    public class SwissbitCloudV2Exception : ScuException
    {
        public int ErrorCode { get; set; }
        public string Operation { get; set; }

        public SwissbitCloudV2Exception() { }

        public SwissbitCloudV2Exception(string message) : base(message) { }

        public SwissbitCloudV2Exception(string message, int errorCode, string operation) : base(message)
        {
            ErrorCode = errorCode;
            Operation = operation;
        }

        public SwissbitCloudV2Exception(string message, Exception innerException) : base(message, innerException) { }

        protected SwissbitCloudV2Exception(SerializationInfo serializationInfo, StreamingContext streamingContext) : base(serializationInfo, streamingContext) { }
    }
}
