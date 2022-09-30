using System;
using System.Runtime.Serialization;

namespace fiskaltrust.Middleware.Localization.QueueME.Exceptions
{
    [Serializable]
    public class InvoiceAlreadyReceivedException : Exception
    {
        public InvoiceAlreadyReceivedException() { }

        public InvoiceAlreadyReceivedException(string message) : base(message) { }

        public InvoiceAlreadyReceivedException(string message, Exception inner) : base(message, inner) { }

        protected InvoiceAlreadyReceivedException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }
}
