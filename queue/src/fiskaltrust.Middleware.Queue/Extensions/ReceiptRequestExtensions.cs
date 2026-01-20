using System;
using fiskaltrust.ifPOS.v1;

namespace fiskaltrust.Middleware.Queue.Extensions
{
    public static class ReceiptRequestExtensions
    {
        public static bool IsV2(this ReceiptRequest receiptRequest) => (receiptRequest.ftReceiptCase & 0xF000_0000_0000) == 0x2000_0000_0000;

        private const ulong V1TaggingMask = 0x0000_F000_0000_0000;
        private const ulong V1Tag_0       = 0x0000_0000_0000_0000;
        private const ulong V1Tag_8       = 0x0000_8000_0000_0000;

        public static bool IsV1Tagging(this ReceiptRequest request)
        {
            if (request == null)
            {
                return false;
            }

            return ((ulong)request.ftReceiptCase).IsV1Tagging();
        }

        public static bool IsV1Tagging(this ulong caseValue)
        {
            var tagging = caseValue & V1TaggingMask;
            return tagging == V1Tag_0 || tagging == V1Tag_8;
        }

        public static void EnsureV1Tagging(this ReceiptRequest request)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            EnsureV1Tagging((ulong)request.ftReceiptCase, nameof(request.ftReceiptCase));

            if (request.cbChargeItems != null)
            {
                foreach (var chargeItem in request.cbChargeItems)
                {
                    EnsureV1Tagging((ulong)chargeItem.ftChargeItemCase, nameof(chargeItem.ftChargeItemCase));
                }
            }

            if (request.cbPayItems != null)
            {
                foreach (var payItem in request.cbPayItems)
                {
                    EnsureV1Tagging((ulong)payItem.ftPayItemCase, nameof(payItem.ftPayItemCase));
                }
            }
        }

        private static void EnsureV1Tagging(ulong caseValue, string componentName)
        {
            var tagging = caseValue & V1TaggingMask;
            if (tagging != V1Tag_0 && tagging != V1Tag_8)
            {
                throw new NotSupportedException($"Unsupported tagging version in {componentName} for localization v1. " + $"Only v1 tagging (0x0) and v1 receipt request tagging (0x8) are supported. " + $"Found: 0x{tagging:X}");
            }
        }
    }
}
