using System;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.Middleware.Contracts.Exceptions;


namespace fiskaltrust.Middleware.Localization.QueueDEFAULT.Extensions
{
    public static class ReceiptRequestExtensions
    {
        public static bool IsFlagSet(this ReceiptRequest receiptRequest)
        {
            return (receiptRequest.ftReceiptCase & 0x10000) > 0;
        }
    }
}