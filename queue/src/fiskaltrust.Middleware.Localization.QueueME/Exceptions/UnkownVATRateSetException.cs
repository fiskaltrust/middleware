using System;

namespace fiskaltrust.Middleware.Localization.QueueME.Exceptions
{
    public class UnkownVATRateSetException : Exception
    {
        public UnkownVATRateSetException()
        {
        }

        public UnkownVATRateSetException(string message)
            : base(message)
        {
        }

        public UnkownVATRateSetException(string message, Exception inner)
            : base(message, inner)
        {
        }
    }
}
