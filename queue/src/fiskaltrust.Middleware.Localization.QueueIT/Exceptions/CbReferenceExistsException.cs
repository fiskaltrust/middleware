using System;
using System.Runtime.Serialization;

namespace fiskaltrust.Middleware.Localization.QueueIT.Exceptions
{
    [Serializable]
    public class CbReferenceExistsException : ArgumentException
    {
        public static readonly string _message = "CbReference {0} from Request already exists in the database, use an unique cbReference for each transaction.";
        public CbReferenceExistsException() { }

        public CbReferenceExistsException(string reference) : base(string.Format(_message, reference)) { }
        public CbReferenceExistsException(string reference, Exception inner) : base(string.Format(_message, reference), inner) { }
        protected CbReferenceExistsException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }
}
