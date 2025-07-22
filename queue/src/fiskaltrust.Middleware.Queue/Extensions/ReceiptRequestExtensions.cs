using fiskaltrust.ifPOS.v1;

namespace fiskaltrust.Middleware.Queue.Extensions
{
    public static class ReceiptRequestExtensions
    {
        public static bool IsV2(this ReceiptRequest receiptRequest) => (receiptRequest.ftReceiptCase & 0x0000_2000_0000_0000L) > 0;

    }
}
