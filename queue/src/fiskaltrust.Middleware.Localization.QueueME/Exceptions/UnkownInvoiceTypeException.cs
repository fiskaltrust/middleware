using System;

namespace fiskaltrust.Middleware.Localization.QueueME.Exceptions
{
    public class UnkownInvoiceTypeException : Exception
    {
        public UnkownInvoiceTypeException()
        {
        }

        public UnkownInvoiceTypeException(string message)
            : base(message)
        {
        }

        public UnkownInvoiceTypeException(string message, Exception inner)
            : base(message, inner)
        {
        }
    }
}
