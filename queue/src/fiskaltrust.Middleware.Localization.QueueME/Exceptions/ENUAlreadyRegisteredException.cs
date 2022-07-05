using System;

namespace fiskaltrust.Middleware.Localization.QueueME.Exceptions
{
    public class EnuAlreadyRegisteredException : Exception
    {
        public EnuAlreadyRegisteredException()
        {
        }
        public EnuAlreadyRegisteredException(string message)
            : base(message)
        {
        }
    }
}
