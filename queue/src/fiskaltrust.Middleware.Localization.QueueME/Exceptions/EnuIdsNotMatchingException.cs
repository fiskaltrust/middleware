using System;

namespace fiskaltrust.Middleware.Localization.QueueME.Exceptions
{
    public class EnuIdsNotMatchingException : Exception
    {
        public EnuIdsNotMatchingException(string message)
            : base(message)
        {
        }
    }
}
