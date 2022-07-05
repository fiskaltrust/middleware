
using System;

namespace fiskaltrust.Middleware.Localization.QueueME.Exceptions
{
    public class UnknownPaymentMethodTypeException : Exception
    {
        public UnknownPaymentMethodTypeException(string message)
            : base(message)
        {
        }
    }
}
