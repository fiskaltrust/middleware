using fiskaltrust.ifPOS.v1;
using fiskaltrust.ifPOS.v1.it;

namespace fiskaltrust.Middleware.Queue.Extensions
{
    public static class ReceiptResponseExtensions
    {
        public static bool IsFailed(this ReceiptResponse receiptResponse) => (receiptResponse.ftState & 0xFFFF_FFFF) == 0xEEEE_EEEE;
    }
}
