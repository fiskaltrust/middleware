using fiskaltrust.ifPOS.v1;

namespace fiskaltrust.Middleware.Localization.QueueAT.Extensions
{
#pragma warning disable
    public static class ReceiptRequestExtensions
    {
        public static bool HasFailedReceiptFlag(this ReceiptRequest receiptRequest)
        {
            return ((receiptRequest.ftReceiptCase & 0x0000_0000_0001_0000) > 0x0000);
        }

        public static bool HasTrainingReceiptFlag(this ReceiptRequest receiptRequest)
        {
            return ((receiptRequest.ftReceiptCase & 0x0000_0000_0002_0000) > 0x0000);
        }

        public static bool HasVoidReceiptFlag(this ReceiptRequest receiptRequest)
        {
            return ((receiptRequest.ftReceiptCase & 0x0000_0000_0004_0000) > 0x0000);
        }

        public static bool HasHandwrittenReceiptFlag(this ReceiptRequest receiptRequest)
        {
            return ((receiptRequest.ftReceiptCase & 0x0000_0000_0008_0000) > 0x0000);
        }

        public static bool IsZeroReceipt(this ReceiptRequest receiptRequest)
        {
            return ((receiptRequest.ftReceiptCase & 0x0000_0000_0000_FFFF) == 0x0000_0000_0000_0002);
        }
        
        public static bool IsInitialOperationReceipt(this ReceiptRequest receiptRequest)
        {
            return ((receiptRequest.ftReceiptCase & 0x0000_0000_0000_FFFF) == 0x0000_0000_0000_0003);
        }

        public static bool IsOutOfOperationReceipt(this ReceiptRequest receiptRequest)
        {
            return ((receiptRequest.ftReceiptCase & 0x0000_0000_0000_FFFF) == 0x0000_0000_0000_0004);
        }
    }
}
