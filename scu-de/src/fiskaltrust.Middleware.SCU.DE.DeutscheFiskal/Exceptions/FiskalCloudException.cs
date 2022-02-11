using System;
using System.Runtime.Serialization;
using fiskaltrust.ifPOS.v1;

namespace fiskaltrust.Middleware.SCU.DE.DeutscheFiskal.Exceptions
{
    [Serializable]
    public class FiskalCloudException : ScuException
    {
        public int ErrorCode { get; set; }
        public string ErrorType { get; set; }
        public string Operation { get; set; }
        
        public FiskalCloudException() { }
        
        public FiskalCloudException(string message) : base(message) { }

        public FiskalCloudException(string message, int errorCode, string errorType, string operation) : base(message) 
        {
            ErrorCode = errorCode;
            ErrorType = errorType;
            Operation = operation;
        }

        public FiskalCloudException(string message, Exception innerException) : base(message, innerException) { }

        protected FiskalCloudException(SerializationInfo serializationInfo, StreamingContext streamingContext) : base(serializationInfo, streamingContext) { }
    }
}
