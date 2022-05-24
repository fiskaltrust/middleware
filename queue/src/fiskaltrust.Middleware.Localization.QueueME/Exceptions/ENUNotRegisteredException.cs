using System;

namespace fiskaltrust.Middleware.Localization.QueueME.Exceptions
{
    public class ENUNotRegisteredException : Exception
    {
        public ENUNotRegisteredException()
        {
        }

        public ENUNotRegisteredException(string message)
            : base(message)
        {
        }

        public ENUNotRegisteredException(string message, Exception inner)
            : base(message, inner)
        {
        }
    }
}
