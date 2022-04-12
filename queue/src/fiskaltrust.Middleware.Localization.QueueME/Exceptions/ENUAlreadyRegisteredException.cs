using System;

namespace fiskaltrust.Middleware.Localization.QueueME.Exceptions
{
    public class ENUAlreadyRegisteredException : Exception
    {
        public ENUAlreadyRegisteredException()
        {
        }

        public ENUAlreadyRegisteredException(string message)
            : base(message)
        {
        }

        public ENUAlreadyRegisteredException(string message, Exception inner)
            : base(message, inner)
        {
        }
    }
}
