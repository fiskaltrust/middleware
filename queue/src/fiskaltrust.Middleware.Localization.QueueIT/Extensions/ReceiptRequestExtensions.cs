using fiskaltrust.ifPOS.v1;

namespace fiskaltrust.Middleware.Localization.QueueIT.Extensions
{
    public static class ReceiptRequestExtensions
    {

        public static bool IsVoid(this ReceiptRequest receiptRequest) => (receiptRequest.ftReceiptCase & 0x0000_0000_000F_0000) == 0x0000_0000_0004_0000;

        public static bool IsRefund(this ReceiptRequest receiptRequest) => (receiptRequest.ftReceiptCase & 0x0000_0000_0F00_0000) == 0x0000_0000_0100_0000;

        public static bool IsInitialOperation(this ReceiptRequest receiptRequest) => (receiptRequest.ftReceiptCase & 0x0000_0000_0000_FFFF) == 0x4001;

        public static bool IsReceiptOperation(this ReceiptRequest receiptRequest) => (receiptRequest.ftReceiptCase & 0x0000_0000_0000_F000) == 0x0000;

        public static bool IsInvoiceOperation(this ReceiptRequest receiptRequest) => (receiptRequest.ftReceiptCase & 0x0000_0000_0000_F000) == 0x1000;

        public static bool IsProtocolOperation(this ReceiptRequest receiptRequest) => (receiptRequest.ftReceiptCase & 0x0000_0000_0000_F000) == 0x2000;
        
        public static bool IsDailyOperation(this ReceiptRequest receiptRequest) => (receiptRequest.ftReceiptCase & 0x0000_0000_0000_F000) == 0x3000;

        public static bool IsLifeCycleOperation(this ReceiptRequest receiptRequest) => (receiptRequest.ftReceiptCase & 0x0000_0000_0000_F000) == 0x4000;
    }
}
