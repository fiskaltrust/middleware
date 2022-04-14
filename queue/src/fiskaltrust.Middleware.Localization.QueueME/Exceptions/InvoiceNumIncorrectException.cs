using System;

namespace fiskaltrust.Middleware.Localization.QueueME.Exceptions
{
    public class InvoiceNumIncorrectException : Exception
    {
        public InvoiceNumIncorrectException()
        {
        }

        public InvoiceNumIncorrectException(string message)
            : base(message)
        {
        }

        public InvoiceNumIncorrectException(string message, Exception inner)
            : base(message, inner)
        {
        }
    }
}
