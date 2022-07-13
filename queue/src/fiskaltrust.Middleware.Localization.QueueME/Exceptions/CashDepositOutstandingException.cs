using System;
using System.Runtime.Serialization;

namespace fiskaltrust.Middleware.Localization.QueueME.Exceptions
{
    [Serializable]
    public class CashDepositOutstandingException : Exception
    {
        public CashDepositOutstandingException() { }

        public CashDepositOutstandingException(string message) : base(message) { }

        public CashDepositOutstandingException(string message, Exception inner) : base(message, inner) { }

        protected CashDepositOutstandingException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }
}
