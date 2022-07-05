using System;

namespace fiskaltrust.Middleware.Localization.QueueME.Exceptions
{
    public class InvoiceNotSignedException : Exception
    {
        public InvoiceNotSignedException(string message)
            : base(message)
        {
        }
    }
}
