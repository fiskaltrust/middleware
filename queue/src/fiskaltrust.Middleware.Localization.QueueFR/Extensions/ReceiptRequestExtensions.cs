using System.Linq;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.Middleware.Localization.QueueFR.Constants;
using fiskaltrust.Middleware.Localization.QueueFR.Models;

namespace fiskaltrust.Middleware.Localization.QueueFR.Extensions
{
    public static class ReceiptRequestExtensions
    {
        public static bool IsZeroReceipt(this ReceiptRequest request) => (request.ftReceiptCase & 0xFFFF) == ((long) ReceiptCaseFR.ZeroReceipt & 0xFFFF);

        public static bool HasFailedReceiptFlag(this ReceiptRequest request) => (request.ftReceiptCase & 0x0000000000010000) > 0;
        public static bool HasTrainingReceiptFlag(this ReceiptRequest request) => (request.ftReceiptCase & 0x0000000000020000) != 0;

        public static Totals GetTotals(this ReceiptRequest request)
        {
            var result = new Totals
            {
                Totalizer = request.cbChargeItems?.Sum(c => c.Amount)
            };

            if (request.cbChargeItems != null)
            {
                foreach (var chargeItem in request.cbChargeItems.Where(ci => new long[] {
                        0x0021,
                        0x0007,
                        0x0006,
                        0x0000}.Contains(ci.ftChargeItemCase & 0xFFFF)))
                {
                    if (chargeItem.VATRate == 5.5m)
                    {
                        chargeItem.ftChargeItemCase += 0x1;
                    }
                    else if (chargeItem.VATRate == 10.0m)
                    {
                        chargeItem.ftChargeItemCase += 0x2;
                    }
                    else if (chargeItem.VATRate == 20.0m)
                    {
                        chargeItem.ftChargeItemCase += 0x3;
                    }
                    else if (chargeItem.VATRate == 0.0m)
                    {
                        chargeItem.ftChargeItemCase += 0x5;
                    }
                    else
                    {
                        chargeItem.ftChargeItemCase += 0x4;
                    }
                }

                var normals = request.cbChargeItems.Where(c => new long[] {
                        0x465200000000001E,
                        0x4652000000000019,
                        0x4652000000000014,
                        0x465200000000000F,
                        0x465200000000000A,
                        0x4652000000000003 }.Contains(c.ftChargeItemCase));
                if (normals.Count() > 0)
                {
                    result.CITotalNormal = normals.Sum(c => c.Amount);
                }

                var reduced1 = request.cbChargeItems.Where(c => new long[] {
                        0x465200000000001C,
                        0x4652000000000017,
                        0x4652000000000012,
                        0x465200000000000D,
                        0x4652000000000008,
                        0x4652000000000001 }.Contains(c.ftChargeItemCase));
                if (reduced1.Count() > 0)
                {
                    result.CITotalReduced1 = reduced1.Sum(c => c.Amount);
                }

                var reduced2 = request.cbChargeItems.Where(c => new long[] {
                        0x465200000000001D,
                        0x4652000000000018,
                        0x4652000000000013,
                        0x465200000000000E,
                        0x4652000000000009,
                        0x4652000000000002 }.Contains(c.ftChargeItemCase));
                if (reduced2.Count() > 0)
                {
                    result.CITotalReduced2 = reduced2.Sum(c => c.Amount);
                }

                var reduceds = request.cbChargeItems.Where(c => new long[] {
                        0x465200000000001F,
                        0x465200000000001A,
                        0x4652000000000015,
                        0x4652000000000010,
                        0x465200000000000B,
                        0x4652000000000004 }.Contains(c.ftChargeItemCase));
                if (reduceds.Count() > 0)
                {
                    result.CITotalReducedS = reduceds.Sum(c => c.Amount);
                }

                var zero = request.cbChargeItems.Where(c => new long[] {
                        0x4652000000000020,
                        0x465200000000001B,
                        0x4652000000000016,
                        0x4652000000000011,
                        0x465200000000000C,
                        0x4652000000000005 }.Contains(c.ftChargeItemCase));
                if (zero.Count() > 0)
                {
                    result.CITotalZero = zero.Sum(c => c.Amount);
                }

                var unknown = request.cbChargeItems.Where(c => !normals.Contains(c) && !reduced1.Contains(c) && !reduced2.Contains(c) && !reduceds.Contains(c) && !zero.Contains(c));
                if (unknown.Count() > 0)
                {
                    result.CITotalUnknown = unknown.Sum(c => c.Amount);
                }
            }

            if (request.cbPayItems != null)
            {

                var cash = request.cbPayItems.Where(p => new long[] {
                    0x4652000000000000,
                    0x4652000000000001,
                    0x4652000000000002,
                    0x4652000000000003,
                    0x4652000000000005,
                    0x4652000000000006,
                    0x465200000000000A,
                    0x4652000000000012 }.Contains(p.ftPayItemCase));
                if (cash.Count() > 0)
                {
                    result.PITotalCash = cash.Sum(c => c.Amount);
                }

                var noncash = request.cbPayItems.Where(p => new long[] {
                    0x4652000000000004,
                    0x4652000000000007,
                    0x4652000000000008,
                    0x4652000000000009,
                    0x465200000000000C,
                    0x465200000000000D }.Contains(p.ftPayItemCase));
                if (noncash.Count() > 0)
                {
                    result.PITotalNonCash = noncash.Sum(c => c.Amount);
                }

                var internals = request.cbPayItems.Where(p => new long[] {
                    0x465200000000000B,
                    0x465200000000000E,
                    0x465200000000000F,
                    0x4652000000000010,
                    0x4652000000000011 }.Contains(p.ftPayItemCase));
                if (internals.Count() > 0)
                {
                    result.PITotalInternal = internals.Sum(c => c.Amount);
                }

                var piunknown = request.cbPayItems.Where(c => !cash.Contains(c) && !noncash.Contains(c) && !internals.Contains(c));
                if (piunknown.Count() > 0)
                {
                    result.PITotalUnknown = piunknown.Sum(c => c.Amount);
                }
            }

            return result;
        }
    }
}
