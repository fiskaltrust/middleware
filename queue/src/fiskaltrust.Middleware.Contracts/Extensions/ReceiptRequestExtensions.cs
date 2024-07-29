using System.Linq;
using fiskaltrust.ifPOS.v1;

namespace fiskaltrust.Middleware.Contracts.Extensions
{
    public static class ReceiptRequestExtensions
    {
        public static bool HasChargeItems(this ReceiptRequest request)
        {
            return request.cbChargeItems != null && request.cbChargeItems.Any();
        }
        public static bool HasPayItems(this ReceiptRequest request)
        {
            return request.cbPayItems != null && request.cbPayItems.Any();
        }
    }
}
