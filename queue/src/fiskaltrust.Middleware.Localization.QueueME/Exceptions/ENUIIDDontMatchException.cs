using System;

namespace fiskaltrust.Middleware.Localization.QueueME.Exceptions
{
    public class ENUIIDDontMatchException : Exception
    {
        public ENUIIDDontMatchException()
        {
        }

        public ENUIIDDontMatchException(string message)
            : base(message)
        {
        }

        public ENUIIDDontMatchException(string message, Exception inner)
            : base(message, inner)
        {
        }
    }
}
