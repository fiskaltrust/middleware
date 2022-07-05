using System;

namespace fiskaltrust.Middleware.Localization.QueueME.Exceptions
{
    public class ReferenceNotSetException : Exception
    {
        public ReferenceNotSetException(string message)
            : base(message)
        {
        }
    }
}
