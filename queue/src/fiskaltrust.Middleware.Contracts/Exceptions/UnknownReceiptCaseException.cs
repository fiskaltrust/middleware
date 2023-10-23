using System;
using System.Runtime.Serialization;

namespace fiskaltrust.Middleware.Contracts.Exceptions
{
    [Serializable]
    public class UnknownReceiptCaseException : ArgumentException
    {
        private static readonly string _message = "The given ReceiptCase 0x{0:x} is not supported. Please refer to docs.fiskaltrust.cloud for supported cases.";
        public UnknownReceiptCaseException() { }

        public UnknownReceiptCaseException(long receiptCase) : base(string.Format(_message, receiptCase)) { }
        public UnknownReceiptCaseException(string message, Exception inner) : base(message, inner) { }
        protected UnknownReceiptCaseException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }
}
