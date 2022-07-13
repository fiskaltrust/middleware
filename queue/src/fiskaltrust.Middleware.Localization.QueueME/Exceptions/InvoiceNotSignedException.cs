using System;
using System.Runtime.Serialization;

namespace fiskaltrust.Middleware.Localization.QueueME.Exceptions
{
    [Serializable]
    public class InvoiceNotSignedException : Exception
    {
        public InvoiceNotSignedException() { }

        public InvoiceNotSignedException(string message) : base(message) { }

        public InvoiceNotSignedException(string message, Exception inner) : base(message, inner) { }

        protected InvoiceNotSignedException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }
}
