using System;

namespace fiskaltrust.Middleware.Localization.QueueME.Exceptions
{
    public class MaxInvoiceItemsExceededException : Exception
    {
        public MaxInvoiceItemsExceededException()
        {
        }

        public MaxInvoiceItemsExceededException(string message)
            : base(message)
        {
        }

        public MaxInvoiceItemsExceededException(string message, Exception inner)
            : base(message, inner)
        {
        }
    }
}
