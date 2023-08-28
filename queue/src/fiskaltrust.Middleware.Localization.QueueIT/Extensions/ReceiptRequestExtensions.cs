using fiskaltrust.ifPOS.v1;

namespace fiskaltrust.Middleware.Localization.QueueIT.Extensions
{
    public static class ReceiptRequestExtensions
    {
        public static bool IsInitialOperationReceipt(this ReceiptRequest receiptRequest)
        {
            return (receiptRequest.ftReceiptCase & 0x0000_0000_0000_FFFF) == 0x0000_0000_0000_0003;
        }
    }
}

