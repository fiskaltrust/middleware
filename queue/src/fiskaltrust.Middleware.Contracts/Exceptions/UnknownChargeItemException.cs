using System;
using System.Runtime.Serialization;

namespace fiskaltrust.Middleware.Contracts.Exceptions
{
    [Serializable]
    public class UnknownChargeItemException : ArgumentException
    {
        private static readonly string _message = "The given ChargeItemCase 0x{0:x} is not supported. Please refer to docs.fiskaltrust.cloud for supported cases.";
        public UnknownChargeItemException() { }

        public UnknownChargeItemException(long chargeItemCase) : base(string.Format(_message, chargeItemCase)) { }
        public UnknownChargeItemException(string message, Exception inner) : base(message, inner) { }
        protected UnknownChargeItemException(SerializationInfo info, StreamingContext context) : base(info, context) { }

    }
}
