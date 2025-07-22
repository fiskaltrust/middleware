using fiskaltrust.ifPOS.v1;

namespace fiskaltrust.Middleware.Queue.Extensions
{
    public static class ReceiptResponseExtensions
    {
        public static bool IsError(this ReceiptResponse receiptResponse) => (receiptResponse.ftState & 0xFFFF_FFFF) == 0xEEEE_EEEE;
    }
}
