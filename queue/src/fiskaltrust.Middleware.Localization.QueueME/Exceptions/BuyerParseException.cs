using System;

namespace fiskaltrust.Middleware.Localization.QueueME.Exceptions
{
    public class BuyerParseException : Exception
    {
        public BuyerParseException()
        {
        }

        public BuyerParseException(string message)
            : base(message)
        {
        }

        public BuyerParseException(string message, Exception inner)
            : base(message, inner)
        {
        }
    }
}
