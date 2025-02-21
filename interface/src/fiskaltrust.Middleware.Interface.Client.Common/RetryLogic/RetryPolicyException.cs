using System;
using System.Runtime.Serialization;

namespace fiskaltrust.Middleware.Interface.Client
{
    [Serializable]
    public class RetryPolicyException : Exception
    {
        public RetryPolicyException(string message) : base(message) { }

        public RetryPolicyException(string message, Exception innerException) : base(message, innerException) { }

        public RetryPolicyException() : base() { }

        protected RetryPolicyException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }
}
