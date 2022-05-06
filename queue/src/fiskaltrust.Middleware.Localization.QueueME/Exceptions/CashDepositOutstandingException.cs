using System;

namespace fiskaltrust.Middleware.Localization.QueueME.Exceptions
{
    public class CashDepositOutstandingException : Exception
    {
        public CashDepositOutstandingException()
        {
        }

        public CashDepositOutstandingException(string message)
            : base(message)
        {
        }

        public CashDepositOutstandingException(string message, Exception inner)
            : base(message, inner)
        {
        }
    }
}
