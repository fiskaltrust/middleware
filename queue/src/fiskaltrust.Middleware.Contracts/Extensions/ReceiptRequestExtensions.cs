using fiskaltrust.ifPOS.v1;

namespace fiskaltrust.Middleware.Contracts.Extensions
{
    public static class ReceiptRequestExtensions
    {
        public static bool IsVoid(this ReceiptRequest receiptRequest)
        {
            return (receiptRequest.ftReceiptCase & 0x0000_0000_0004_0000) > 0x0000;
        }
    }
}
