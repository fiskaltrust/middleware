
using System;

namespace fiskaltrust.Middleware.Localization.QueueME.Exceptions
{
    public class UnknownPaymentMethodeTypeException : Exception
    {
        public UnknownPaymentMethodeTypeException()
        {
        }

        public UnknownPaymentMethodeTypeException(string message)
            : base(message)
        {
        }

        public UnknownPaymentMethodeTypeException(string message, Exception inner)
            : base(message, inner)
        {
        }
    }
}
