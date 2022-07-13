using System;
using System.Runtime.Serialization;

namespace fiskaltrust.Middleware.Localization.QueueME.Exceptions
{
    [Serializable]
    public class UserNotParsableException : Exception
    {
        public UserNotParsableException() { }

        public UserNotParsableException(string message) : base(message) { }

        public UserNotParsableException(string message, Exception inner) : base(message, inner) { }

        protected UserNotParsableException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }
}
