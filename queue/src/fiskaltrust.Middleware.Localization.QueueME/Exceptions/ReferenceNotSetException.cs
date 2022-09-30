using System;
using System.Runtime.Serialization;

namespace fiskaltrust.Middleware.Localization.QueueME.Exceptions
{
    [Serializable]
    public class ReferenceNotSetException : Exception
    {
        public ReferenceNotSetException() { }

        public ReferenceNotSetException(string message) : base(message) { }

        public ReferenceNotSetException(string message, Exception inner) : base(message, inner) { }

        protected ReferenceNotSetException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }
}
