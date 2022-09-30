using System;
using System.Runtime.Serialization;

namespace fiskaltrust.Middleware.Localization.QueueME.Exceptions
{
    [Serializable]
    public class BuyerNotParsableException : Exception
    {
        public BuyerNotParsableException() { }

        public BuyerNotParsableException(string message) : base(message) { }

        public BuyerNotParsableException(string message, Exception inner) : base(message, inner) { }

        protected BuyerNotParsableException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }
}
