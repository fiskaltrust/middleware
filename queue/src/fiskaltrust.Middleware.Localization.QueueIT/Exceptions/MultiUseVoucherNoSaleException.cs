using System;
using System.Runtime.Serialization;

namespace fiskaltrust.Middleware.Localization.QueueIT.Exceptions
{
    [Serializable]
    public class MultiUseVoucherNoSaleException : ArgumentException
    {
        public static readonly string _message = "In a multi use Voucher sale no other chargeitems can be sold!";

        public MultiUseVoucherNoSaleException() : base(_message) { }
        public MultiUseVoucherNoSaleException(Exception inner) : base(_message, inner) { }
        protected MultiUseVoucherNoSaleException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }
}
