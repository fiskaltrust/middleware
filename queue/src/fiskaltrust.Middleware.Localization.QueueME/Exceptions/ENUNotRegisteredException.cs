using System;

namespace fiskaltrust.Middleware.Localization.QueueME.Exceptions
{
    public class EnuNotRegisteredException : Exception
    {
        public EnuNotRegisteredException()
        {
        }

        public EnuNotRegisteredException(string message)
            : base(message)
        {
        }
    }
}
