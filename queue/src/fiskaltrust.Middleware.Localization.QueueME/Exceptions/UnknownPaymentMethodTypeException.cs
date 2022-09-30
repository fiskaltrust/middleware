using System;
using System.Runtime.Serialization;

namespace fiskaltrust.Middleware.Localization.QueueME.Exceptions
{
    [Serializable]
    public class UnknownPaymentMethodTypeException : Exception
    {
        public UnknownPaymentMethodTypeException() { }

        public UnknownPaymentMethodTypeException(string message) : base(message) { }

        public UnknownPaymentMethodTypeException(string message, Exception inner) : base(message, inner) { }

        protected UnknownPaymentMethodTypeException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }
}
