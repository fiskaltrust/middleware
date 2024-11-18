using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.Middleware.Localization.QueueDE.Constants;
using Microsoft.Extensions.Logging;

namespace fiskaltrust.Middleware.Localization.QueueDE.Extensions
{
#pragma warning disable
    public static class ReceiptRequestExtensions
    {
        public static bool IsImplictFlow(this ReceiptRequest receiptRequest)
        {
            return (receiptRequest.ftReceiptCase & 0x1_0000_0000) == 0x1_0000_0000;
        }

        public static bool IsUsedFailed(this ReceiptRequest receiptRequest)
        {
            return ((receiptRequest.ftReceiptCase & 0x0000_0000_0001_0000) > 0x0000);
        }

        public static bool IsTraining(this ReceiptRequest receiptRequest)
        {
            return ((receiptRequest.ftReceiptCase & 0x0000_0000_0002_0000) > 0x0000);
        }

        public static bool IsReverse(this ReceiptRequest receiptRequest)
        {
            return ((receiptRequest.ftReceiptCase & 0x0000_0000_0004_0000) > 0x0000);
        }

        public static bool IsHandwritten(this ReceiptRequest receiptRequest)
        {
            return ((receiptRequest.ftReceiptCase & 0x0000_0000_0008_0000) > 0x0000);
        }

        public static bool HasFailOnOpenTransactionsFlag(this ReceiptRequest receiptRequest)
        {
            return ((receiptRequest.ftReceiptCase & 0x0000_0000_1000_0000) > 0x0000);
        }
        public static bool IsTseInfoRequest(this ReceiptRequest receiptRequest)
        {
            return ((receiptRequest.ftReceiptCase & 0x0000_0000_0080_0000) > 0x0000);
        }

        public static bool IsTseSelftestRequest(this ReceiptRequest receiptRequest)
        {
            return ((receiptRequest.ftReceiptCase & 0x0000_0000_0100_0000) > 0x0000);
        }

        public static bool IsModifyClientIdOnlyRequest(this ReceiptRequest receiptRequest)
        {
            return ((receiptRequest.ftReceiptCase & 0x0000_0000_0100_0000) > 0x0000);
        }

        public static bool IsTseTarDownloadRequest(this ReceiptRequest receiptRequest)
        {
            return ((receiptRequest.ftReceiptCase & 0x0000_0000_0200_0000) > 0x0000);
        }

        public static bool IsTseTarDownloadBypass(this ReceiptRequest receiptRequest)
        {
            return ((receiptRequest.ftReceiptCase & 0x0000_0000_0400_0000) > 0x0000);
        }

        public static bool IsMasterDataUpdate(this ReceiptRequest receiptRequest)
        {
            return ((receiptRequest.ftReceiptCase & 0x0000_0000_0800_0000) > 0x0000);
        }

        public static bool IsRemoveOpenTransactionsWhichAreNotOnTse(this ReceiptRequest receiptRequest)
        {
            return ((receiptRequest.ftReceiptCase & 0x0000_0000_2000_0000) > 0x0000);
        }
        public static bool IsInitiateScuSwitchReceiptForce(this ReceiptRequest receiptRequest)
        {
            return ((receiptRequest.ftReceiptCase & 0x0000_0000_4000_0000) > 0x0000);
        }

        public static bool IsInitialOperationReceipt(this ReceiptRequest receiptRequest)
        {
            return ((receiptRequest.ftReceiptCase & 0x0000_0000_0000_FFFF) == 0x0000_0000_0000_0003);
        }

        public static bool IsOutOfOperationReceipt(this ReceiptRequest receiptRequest)
        {
            return ((receiptRequest.ftReceiptCase & 0x0000_0000_0000_FFFF) == 0x0000_0000_0000_0004);
        }

        public static bool IsZeroReceipt(this ReceiptRequest receiptRequest)
        {
            return ((receiptRequest.ftReceiptCase & 0x0000_0000_0000_FFFF) == 0x0000_0000_0000_0002);
        }

        public static bool IsInitiateScuSwitchReceipt(this ReceiptRequest receiptRequest)
        {
            return ((receiptRequest.ftReceiptCase & 0x0000_0000_0000_FFFF) == 0x0000_0000_0000_0017);
        }

        public static bool IsFinishScuSwitchReceipt(this ReceiptRequest receiptRequest)
        {
            return ((receiptRequest.ftReceiptCase & 0x0000_0000_0000_FFFF) == 0x0000_0000_0000_0018);
        }

        public static bool IsFailTransactionReceipt(this ReceiptRequest receiptRequest)
        {
            return ((receiptRequest.ftReceiptCase & 0x0000_0000_0000_FFFF) == 0x0000_0000_0000_000B);
        }

        public static bool IsMigrationReceipt(this ReceiptRequest receiptRequest)
        {
            return ((receiptRequest.ftReceiptCase & 0x0000_0000_0000_FFFF) == 0x0000_0000_0000_0019);
        }

        public static bool IsVoid(this ReceiptRequest receiptRequest)
        {
            return (receiptRequest.ftReceiptCase & 0x0000_0000_0004_0000) > 0x0000;
        }

        public static string GetReceiptIdentification(this ReceiptRequest receiptRequest, long receiptNumerator, ulong? transactionNumber)
        {
            /*
             * IT for implicit-flow
             * ST for start-transaction
             * UT for update-transaction
             * T for everywhere, where a finish-transaction is involved
             * nothing for all where
             *   - SCU is not reachable
             *   - SCU communication is not required
             *   - aso
             */
            if (transactionNumber.HasValue)
            {
                //start-transaction
                if ((receiptRequest.ftReceiptCase & 0xFFFF) == 0x0008)
                {
                    return $"ft{receiptNumerator:X}#ST{transactionNumber}";
                }
                //update-transaction
                if ((receiptRequest.ftReceiptCase & 0xFFFF) == 0x0009)
                {
                    return $"ft{receiptNumerator:X}#UT{transactionNumber}";
                }
                //delta-transaction
                if ((receiptRequest.ftReceiptCase & 0xFFFF) == 0x000A)
                {
                    return $"ft{receiptNumerator:X}#DT{transactionNumber}";
                }

                //implicit-flow
                if (receiptRequest.IsImplictFlow())
                {
                    return $"ft{receiptNumerator:X}#IT{transactionNumber}";
                }

                //everything else
                return $"ft{receiptNumerator:X}#T{transactionNumber}";
            }
            else
            {
                return $"ft{receiptNumerator:X}#";
            }
        }

        public static string GetTseProcessType(this ReceiptRequest request)
        {
            return (request.ftReceiptCase & 0xFFFF) switch
            {
                0x0000 => DSFinVKConstants.PROCESS_TYPE_KASSENBELEG_V1,
                0x0001 => DSFinVKConstants.PROCESS_TYPE_KASSENBELEG_V1,
                0x0002 => DSFinVKConstants.PROCESS_TYPE_SONSTIGER_VORGANG,
                0x0003 => DSFinVKConstants.PROCESS_TYPE_SONSTIGER_VORGANG,
                0x0004 => DSFinVKConstants.PROCESS_TYPE_SONSTIGER_VORGANG,
                0x0005 => DSFinVKConstants.PROCESS_TYPE_SONSTIGER_VORGANG,
                0x0006 => DSFinVKConstants.PROCESS_TYPE_SONSTIGER_VORGANG,
                0x0007 => DSFinVKConstants.PROCESS_TYPE_SONSTIGER_VORGANG,
                0x0008 => DSFinVKConstants.PROCESS_TYPE_EMPTY,
                0x0009 => DSFinVKConstants.PROCESS_TYPE_EMPTY,
                0x000A => DSFinVKConstants.PROCESS_TYPE_EMPTY,
                0x000B => DSFinVKConstants.PROCESS_TYPE_KASSENBELEG_V1,
                0x000C => DSFinVKConstants.PROCESS_TYPE_KASSENBELEG_V1,
                0x000D => DSFinVKConstants.PROCESS_TYPE_KASSENBELEG_V1,
                0x000E => DSFinVKConstants.PROCESS_TYPE_KASSENBELEG_V1,
                0x000F => DSFinVKConstants.PROCESS_TYPE_KASSENBELEG_V1,
                0x0010 => (request.cbPayItems ?? Array.Empty<PayItem>()).Any() ? DSFinVKConstants.PROCESS_TYPE_KASSENBELEG_V1 : DSFinVKConstants.PROCESS_TYPE_BESTELLUNG_V1,
                0x0011 => DSFinVKConstants.PROCESS_TYPE_KASSENBELEG_V1,
                0x0012 => DSFinVKConstants.PROCESS_TYPE_KASSENBELEG_V1,
                0x0013 => (request.cbChargeItems ?? Array.Empty<ChargeItem>()).Any() && (request.cbPayItems ?? Array.Empty<PayItem>()).Any() ? DSFinVKConstants.PROCESS_TYPE_KASSENBELEG_V1 : DSFinVKConstants.PROCESS_TYPE_SONSTIGER_VORGANG,
                0x0014 => DSFinVKConstants.PROCESS_TYPE_SONSTIGER_VORGANG,
                0x0015 => DSFinVKConstants.PROCESS_TYPE_KASSENBELEG_V1,
                0x0016 => DSFinVKConstants.PROCESS_TYPE_KASSENBELEG_V1,
                0x0017 => DSFinVKConstants.PROCESS_TYPE_SONSTIGER_VORGANG,
                0x0018 => DSFinVKConstants.PROCESS_TYPE_SONSTIGER_VORGANG,
                _ => DSFinVKConstants.PROCESS_TYPE_KASSENBELEG_V1
            };
        }

        public static string GetDescription(this ReceiptRequest request)
        {
            return (request.ftReceiptCase & 0xFFFF) switch
            {
                0x0000 => "unknown ",
                0x0001 => "pos-receipt",
                0x0002 => "zero-receipt",
                0x0003 => "initial operation receipt / start-receipt",
                0x0004 => "out of operation receipt / stop-receipt",
                0x0005 => "monthly-closing",
                0x0006 => "yearly-closing",
                0x0007 => "daily-closing",
                0x0008 => "start-transaction-receipt",
                0x0009 => "update-transaction-receipt",
                0x000A => "delta-transaction-receipt",
                0x000B => "fail-transaction-receipt",
                0x000C => "b2b-invoice",
                0x000D => "b2c-invoice",
                0x000E => "info-invoice",
                0x000F => "info-delivery-note",
                0x0010 => "info-order",
                0x0011 => "cash deposit / cash pay-in / cash pay-out / exchange",
                0x0012 => "material consumption",
                0x0013 => "info-internal",
                0x0014 => "protocol",
                0x0015 => "foreign sales",
                0x0016 => "void-receipt",
                0x0017 => "initiate-scu-switch",
                0x0018 => "finish-scu-switch",
                _ => throw new NotImplementedException($"The given ftReceiptCase {request.ftReceiptCase} is not yet supported.")
            };
        }

        public static bool IsReceiptProcessType(this ReceiptRequest request) => request.GetTseProcessType() == DSFinVKConstants.PROCESS_TYPE_KASSENBELEG_V1;

        public static bool IsOrderProcessType(this ReceiptRequest request) => request.GetTseProcessType() == DSFinVKConstants.PROCESS_TYPE_BESTELLUNG_V1;

        public static bool IsOtherProcessType(this ReceiptRequest request) => request.GetTseProcessType() == DSFinVKConstants.PROCESS_TYPE_SONSTIGER_VORGANG;

        public static string GetReceiptTransactionType(this ReceiptRequest request)
        {
            if ((request.ftReceiptCase & 0x0000000000020000) == 0x0000000000020000)
            {
                return DSFinVKConstants.BON_TYP_OTHERACTION_TRAINING;
            }

            return (request.ftReceiptCase & 0xFFFF) switch
            {
                0x0000 => DSFinVKConstants.BON_TYP_RECEIPT,
                0x0001 => DSFinVKConstants.BON_TYP_RECEIPT,
                0x0002 => DSFinVKConstants.BON_TYP_NONE,
                0x0003 => DSFinVKConstants.BON_TYP_NONE,
                0x0004 => DSFinVKConstants.BON_TYP_NONE,
                0x0005 => DSFinVKConstants.BON_TYP_NONE,
                0x0006 => DSFinVKConstants.BON_TYP_NONE,
                0x0007 => DSFinVKConstants.BON_TYP_NONE,
                0x0008 => DSFinVKConstants.BON_TYP_NONE,
                0x0009 => DSFinVKConstants.BON_TYP_NONE,
                0x000A => DSFinVKConstants.BON_TYP_NONE,
                0x000B => DSFinVKConstants.BON_TYP_OTHERACTION_FAILED,
                0x000C => DSFinVKConstants.BON_TYP_RECEIPT,
                0x000D => DSFinVKConstants.BON_TYP_RECEIPT,
                0x000E => DSFinVKConstants.BON_TYP_OTHERACTION_INVOICE,
                0x000F => DSFinVKConstants.BON_TYP_OTHERACTION_TRANSFER,
                0x0010 => (request.cbPayItems ?? Array.Empty<PayItem>()).Any() ? DSFinVKConstants.BON_TYP_OTHERACTION_ORDER : DSFinVKConstants.BON_TYP_NONE,
                0x0011 => DSFinVKConstants.BON_TYP_RECEIPT,
                0x0012 => DSFinVKConstants.BON_TYP_OTHERACTION_CONSUMPTION,
                0x0013 => (request.cbChargeItems ?? Array.Empty<ChargeItem>()).Any() && (request.cbPayItems ?? Array.Empty<PayItem>()).Any() ? DSFinVKConstants.BON_TYP_OTHERACTION_ELSE : DSFinVKConstants.BON_TYP_NONE,
                0x0014 => DSFinVKConstants.BON_TYP_NONE,
                0x0015 => DSFinVKConstants.BON_TYP_OTHERACTION_ELSE,
                0x0016 => DSFinVKConstants.BON_TYP_OTHERACTION_VOIDED,
                0x0017 => DSFinVKConstants.BON_TYP_NONE,
                0x0018 => DSFinVKConstants.BON_TYP_NONE,
                _ => DSFinVKConstants.BON_TYP_RECEIPT
            };
        }

        public static string GetReceiptTaxes(this ReceiptRequest request)
        {
            var normal = 0.0m;
            var discounted_1 = 0.0m;
            var special_1 = 0.0m;
            var special_2 = 0.0m;
            var zero = 0.0m;

            foreach (var item in request.cbChargeItems ?? Array.Empty<ChargeItem>())
            {
                switch (item.ftChargeItemCase & 0xFFFF)
                {
                    case 0x0000:
                        if (item.VATRate == 19.0m)
                        {
                            normal += GetAmount(item.ftChargeItemCase, item.Quantity, item.Amount);
                        }
                        else if (item.VATRate == 7.0m)
                        {
                            discounted_1 += GetAmount(item.ftChargeItemCase, item.Quantity, item.Amount);
                        }
                        else if (item.VATRate == 10.7m)
                        {
                            special_1 += GetAmount(item.ftChargeItemCase, item.Quantity, item.Amount);
                        }
                        else if (item.VATRate == 5.5m)
                        {
                            special_2 += GetAmount(item.ftChargeItemCase, item.Quantity, item.Amount);
                        }
                        else
                        {
                            zero += GetAmount(item.ftChargeItemCase, item.Quantity, item.Amount);
                        }
                        break;
                    case 0x0001:
                    case 0x0011:
                    case 0x0019:
                    case 0x0021:
                    case 0x0029:
                    case 0x0031:
                    case 0x0039:
                    case 0x0041:
                    case 0x0051:
                    case 0x0061:
                    case 0x0069:
                    case 0x0071:
                    case 0x0079:
                    case 0x0081:
                    case 0x0089:
                        normal += GetAmount(item.ftChargeItemCase, item.Quantity, item.Amount);
                        break;
                    case 0x0002:
                    case 0x0012:
                    case 0x001A:
                    case 0x0022:
                    case 0x002A:
                    case 0x0032:
                    case 0x003A:
                    case 0x0042:
                    case 0x0052:
                    case 0x0062:
                    case 0x006A:
                    case 0x0072:
                    case 0x007A:
                    case 0x0082:
                    case 0x008A:
                        discounted_1 += GetAmount(item.ftChargeItemCase, item.Quantity, item.Amount);
                        break;
                    case 0x0003:
                    case 0x0013:
                    case 0x001B:
                    case 0x0023:
                    case 0x002B:
                    case 0x0033:
                    case 0x003B:
                    case 0x0043:
                    case 0x0053:
                    case 0x0063:
                    case 0x006B:
                    case 0x0073:
                    case 0x007B:
                    case 0x0083:
                    case 0x008B:
                        special_1 += GetAmount(item.ftChargeItemCase, item.Quantity, item.Amount);
                        break;
                    case 0x0004:
                    case 0x0014:
                    case 0x001C:
                    case 0x0024:
                    case 0x002C:
                    case 0x0034:
                    case 0x003C:
                    case 0x0044:
                    case 0x0054:
                    case 0x0064:
                    case 0x006C:
                    case 0x0074:
                    case 0x007C:
                    case 0x0084:
                    case 0x008C:
                        special_2 += GetAmount(item.ftChargeItemCase, item.Quantity, item.Amount);
                        break;
                    default:
                        zero += GetAmount(item.ftChargeItemCase, item.Quantity, item.Amount);

                        break;
                }
            }
            foreach (var item in request.cbPayItems ?? Array.Empty<PayItem>())
            {
                switch (item.ftPayItemCase & 0xFFFF)
                {
                    case 0x000A:
                    {
                        zero += item.InverseAmountIfNotVoidReceipt(request.IsVoid());
                        break;
                    }
                    case 0x000D:
                    case 0x000E:
                    case 0x000F:
                    case 0x0010:
                    case 0x0011:
                    case 0x0012:
                    case 0x0013:
                    case 0x0014:
                    case 0x0015:
                    case 0x0016:
                    case 0x0017:
                        item.Amount = GetAmount(item.ftPayItemCase, item.Quantity, item.Amount);
                        zero += item.Amount * -1;
                        break;
                    default:
                        break;
                }
            }

            //TODO check rounding problesm: round sum(results) towards receipt.totalamount if given
            return FormatAmount(normal) + "_" + FormatAmount(discounted_1) + "_" + FormatAmount(special_1) +
                                "_" + FormatAmount(special_2) + "_" + FormatAmount(zero);
        }
        private static string FormatAmount(decimal value)
        {
            return string.Format(CultureInfo.InvariantCulture, "{0:0.00###}", value);
        }
        public static DateTime GetReceiptActionStartMoment(this ReceiptRequest request)
        {
            DateTime GetMinDateTime(IEnumerable<DateTime?> dts)
            {
                return dts == null || !dts.Any()
                    ? DateTime.UtcNow
                    : dts.Select(x => x.HasValue ? x.Value : DateTime.UtcNow).Min();
            }

            return (new DateTime[] {
                GetMinDateTime(request.cbChargeItems?.Select(x => x.Moment)),
                GetMinDateTime(request.cbPayItems?.Select(x => x.Moment)),
                request.cbReceiptMoment
            }).Min();
        }

        public static decimal GetAmount(long payOrChargeCase, decimal quantity, decimal amount)
        {
            if (IsPositionCancellation(payOrChargeCase))
            {
                return amount;
            }
            else
            {
                return quantity < 0 && amount >= 0 ? amount * -1 : amount;
            }
        }

        public static void CheckForEqualSumChargePayItems(this ReceiptRequest request, ILogger logger)
        {
            var chargeAmount = request.cbChargeItems != null ? request.cbChargeItems.Sum(x => x.Amount != null ? GetAmount(x.ftChargeItemCase, x.Quantity, x.Amount) : 0) : 0;
            var payAmount = request.cbPayItems != null ? request.cbPayItems.Sum(x => x.Amount != null ? GetAmount(x.ftPayItemCase, x.Quantity, x.Amount) : 0) : 0;
            if (chargeAmount != payAmount)
            {
                var _differentPayChargeAmount = $"Aggregated sum of ChargeItem amounts ({chargeAmount}) does not match the sum of PayItem amount ({payAmount}). This is usually a hint for an implementation issue. Please see https://docs.fiskaltrust.cloud/docs/poscreators/middleware-doc for more details.";
                logger.LogWarning(_differentPayChargeAmount);
            }
        }

        public static bool IsPositionCancellation(long payOrChargeCase)
        {
            return ((payOrChargeCase & 0x0000_0000_0020_0000) > 0x0000);
        }

    }
}
