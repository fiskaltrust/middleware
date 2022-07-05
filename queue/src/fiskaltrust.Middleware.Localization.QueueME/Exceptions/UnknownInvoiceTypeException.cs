using System;

namespace fiskaltrust.Middleware.Localization.QueueME.Exceptions
{
    public class UnknownInvoiceTypeException : Exception
    {
        public UnknownInvoiceTypeException(string message)
            : base(message)
        {
        }
    }
}
