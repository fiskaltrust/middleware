using System;

namespace fiskaltrust.Middleware.Localization.QueueME.Exceptions
{
    public class ENUValidToNotSetException : Exception
    {
        public ENUValidToNotSetException()
        {
        }

        public ENUValidToNotSetException(string message)
            : base(message)
        {
        }

        public ENUValidToNotSetException(string message, Exception inner)
            : base(message, inner)
        {
        }
    }
}
