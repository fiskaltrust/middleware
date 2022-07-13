using System;

namespace fiskaltrust.Middleware.Localization.QueueME.Exceptions
{
    [Serializable]
    public class UserParseException : Exception
    {
        public UserParseException(string message) : base(message)
        {
        }

        public UserParseException(string message, Exception inner)
            : base(message, inner)
        {
        }
    }
}
