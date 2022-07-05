using System;

namespace fiskaltrust.Middleware.Localization.QueueME.Exceptions
{
    public class InvoiceAlreadyReceivedException : Exception
    {
        public InvoiceAlreadyReceivedException(string message)
            : base(message)
        {
        }
    }
}
