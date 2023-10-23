using System;
using System.Runtime.Serialization;

namespace fiskaltrust.Middleware.Localization.QueueME.Exceptions
{
    [Serializable]
    public class UnknownInvoiceTypeException : Exception
    {
        public UnknownInvoiceTypeException() { }

        public UnknownInvoiceTypeException(string message) : base(message) { }

        public UnknownInvoiceTypeException(string message, Exception inner) : base(message, inner) { }

        protected UnknownInvoiceTypeException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }
}
