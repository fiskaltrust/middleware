using System;
using System.Runtime.Serialization;

namespace fiskaltrust.Middleware.Contracts.Exceptions
{
    [Serializable]
    public class MissiningInitialOpException : ArgumentException
    {
        private static readonly string _message = "Initial-Operation-Receipt is missing. Please send this receipt according to our documentation.";

        public MissiningInitialOpException() : base(_message) { }
        public MissiningInitialOpException(string message, Exception inner) : base(message, inner) { }
        protected MissiningInitialOpException(SerializationInfo info, StreamingContext context) : base(info, context) { }

    }
}
