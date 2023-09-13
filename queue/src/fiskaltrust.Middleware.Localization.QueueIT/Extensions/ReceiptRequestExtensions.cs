using fiskaltrust.ifPOS.v1;

namespace fiskaltrust.Middleware.Localization.QueueIT.Extensions
{
    public static class ReceiptRequestExtensions
    {
        public static bool IsV2Receipt(this ReceiptRequest receiptRequest) => (receiptRequest.ftReceiptCase & 0x0000_F000_0000_0000) == 0x0000_2000_0000_0000;

        public static bool IsVoid(this ReceiptRequest receiptRequest) => (receiptRequest.ftReceiptCase & 0x0000_0000_000F_0000) == 0x0000_0000_0004_0000;

        public static bool IsRefund(this ReceiptRequest receiptRequest) => (receiptRequest.ftReceiptCase & 0x0000_0000_0F00_0000) == 0x0000_0000_0100_0000;
    }
}
