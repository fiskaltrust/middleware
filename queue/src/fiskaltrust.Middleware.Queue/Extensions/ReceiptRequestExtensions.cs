using fiskaltrust.ifPOS.v1;

namespace fiskaltrust.Middleware.Queue.Extensions
{
    public static class ReceiptRequestExtensions
    {
        public static bool IsV2(this ReceiptRequest receiptRequest) => (receiptRequest.ftReceiptCase & 0xF000_0000_0000) == 0x2000_0000_0000;

    }
}
