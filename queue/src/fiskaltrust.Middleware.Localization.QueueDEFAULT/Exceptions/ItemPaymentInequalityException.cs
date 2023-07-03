using System;
using System.Runtime.Serialization;

namespace fiskaltrust.Middleware.Localization.QueueDEFAULT.Exceptions
{
    [Serializable]
    public class ItemPaymentInequalityException : Exception
    {
        public static readonly string _message = "Pament sum of {0} is inequal to chargeitem sum {1}.";
        public ItemPaymentInequalityException() { }

        public ItemPaymentInequalityException(decimal paymentSum, decimal chargeItemSum) : base(string.Format(_message, paymentSum, chargeItemSum)) { }
        public ItemPaymentInequalityException(decimal paymentSum, decimal chargeItemSum, Exception inner) : base(string.Format(_message, paymentSum, chargeItemSum), inner) { }
        protected ItemPaymentInequalityException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }
}
