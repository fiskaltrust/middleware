using System;
using System.Runtime.Serialization;

namespace fiskaltrust.Middleware.Localization.QueueME.Exceptions
{
    [Serializable]
    public class MaxInvoiceItemsExceededException : Exception
    {
        public MaxInvoiceItemsExceededException() { }

        public MaxInvoiceItemsExceededException(string message) : base(message) { }

        public MaxInvoiceItemsExceededException(string message, Exception inner) : base(message, inner) { }

        protected MaxInvoiceItemsExceededException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }
}
