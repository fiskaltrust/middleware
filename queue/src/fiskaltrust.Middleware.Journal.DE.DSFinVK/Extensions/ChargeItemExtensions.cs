using System;
using fiskaltrust.Middleware.Journal.DE.Common.Extensions;
using fiskaltrust.Middleware.Journal.DE.Common.Helpers;
using fiskaltrust.Middleware.Journal.DE.DSFinVK.Constants;
using fiskaltrust.ifPOS.v1;
using Microsoft.Extensions.Logging;

namespace fiskaltrust.Middleware.Journal.DE.DSFinVK.Extensions
{
    public static class ChargeItemExtensions
    {
        public static bool IsInHouseConsumption(this ChargeItem chargeItem) => (chargeItem.ftChargeItemCase & 0xF0000) != 0x10000;

        public static decimal CalculateVAT(this ChargeItem chargeItem, ILogger logger, DateTime cbReceiptMoment) {
            return chargeItem.VATAmount != null ? chargeItem.VATAmount.Value
                   : TaxCalculator.CalculateVAT(chargeItem.Amount, chargeItem.GetVATRate(logger, cbReceiptMoment));
        }

        public static int GetVATKey(this ChargeItem chargeItem, ILogger logger, DateTime cbReceiptMoment) => chargeItem.ftChargeItemCase.GetVATKey(logger, chargeItem.VATRate, chargeItem.Moment ?? cbReceiptMoment);

        public static string GetVATDefinition(this ChargeItem chargeItem, ILogger logger)
        {
            var chargeItemCaseData = SerializationHelper.GetChargeItemCaseData(chargeItem);
            return chargeItemCaseData?.VatDefinitionDescription ?? DSFinVKConstants.VatDefinition.GetVatDefinition(chargeItem.GetVATKey(logger, chargeItem.Moment ?? DateTime.Now));
        }

        public static decimal GetVATRate(this ChargeItem chargeItem, ILogger logger, DateTime cbReceiptMoment) => chargeItem.ftChargeItemCase.GetVATRate(logger, chargeItem.VATRate, chargeItem.Moment ?? cbReceiptMoment);
         
        public static string GetBusinessCaseType(this ChargeItem chargeItem, ILogger logger, ReceiptRequest request = null)
        {
            switch (chargeItem.ftChargeItemCase & 0xFFFF)
            {
                case 0x0000:
                case 0x0001:
                case 0x0002:
                case 0x0003:
                case 0x0004:
                case 0x0005:
                case 0x0006:
                case 0x0007:
                case 0x0011:
                case 0x0012:
                case 0x0013:
                case 0x0014:
                case 0x0015:
                case 0x0016:
                case 0x0017:
                case 0x0019:
                case 0x001A:
                case 0x001B:
                case 0x001C:
                case 0x001D:
                case 0x001E:
                case 0x001F:
                case 0x00A1:
                case 0x00A2:
                    return DSFinVKConstants.BusinessCaseTypes.Revenue;
                case 0x0021:
                case 0x0022:
                case 0x0023:
                case 0x0024:
                case 0x0025:
                case 0x0026:
                case 0x0027:
                    return DSFinVKConstants.BusinessCaseTypes.ReturnableNormal;
                case 0x0029:
                case 0x002A:
                case 0x002B:
                case 0x002C:
                case 0x002D:
                case 0x002E:
                case 0x002F:
                    return DSFinVKConstants.BusinessCaseTypes.ReturnableReverse;
                case 0x0031:
                case 0x0032:
                case 0x0033:
                case 0x0034:
                case 0x0035:
                case 0x0036:
                case 0x0037:
                    return DSFinVKConstants.BusinessCaseTypes.Discount;
                case 0x0039:
                case 0x003A:
                case 0x003B:
                case 0x003C:
                case 0x003D:
                case 0x003E:
                case 0x003F:
                    return DSFinVKConstants.BusinessCaseTypes.ExtraCharge;
                case 0x0041:
                case 0x0042:
                case 0x0043:
                case 0x0044:
                case 0x0045:
                case 0x0046:
                case 0x0047:
                    return DSFinVKConstants.BusinessCaseTypes.GrantNotReal;
                case 0x0049:
                    return DSFinVKConstants.BusinessCaseTypes.GrantReal;
                case 0x0051:
                case 0x0052:
                case 0x0053:
                case 0x0054:
                case 0x0055:
                case 0x0056:
                case 0x0057:
                    return DSFinVKConstants.BusinessCaseTypes.TipEmployer;
                case 0x0059:
                    return DSFinVKConstants.BusinessCaseTypes.TipEmployee;
                case 0x0060:
                    return DSFinVKConstants.BusinessCaseTypes.VoucherPurchase;
                case 0x0061:
                case 0x0062:
                case 0x0063:
                case 0x0064:
                case 0x0065:
                case 0x0066:
                case 0x0067:
                    return DSFinVKConstants.BusinessCaseTypes.CouponPurchase;
                case 0x0068:
                    return DSFinVKConstants.BusinessCaseTypes.VoucherRedemption;
                case 0x0069:
                case 0x006A:
                case 0x006B:
                case 0x006C:
                case 0x006D:
                case 0x006E:
                case 0x006F:
                    return DSFinVKConstants.BusinessCaseTypes.CouponRedemption;
                case 0x0071:
                case 0x0072:
                case 0x0073:
                case 0x0074:
                case 0x0075:
                case 0x0076:
                case 0x0077:
                    return DSFinVKConstants.BusinessCaseTypes.ReceivableCreation;
                case 0x0079:
                case 0x007A:
                case 0x007B:
                case 0x007C:
                case 0x007D:
                case 0x007E:
                case 0x007F:
                    return DSFinVKConstants.BusinessCaseTypes.ReceivableReduction;
                case 0x0081:
                case 0x0082:
                case 0x0083:
                case 0x0084:
                case 0x0085:
                case 0x0086:
                case 0x0087:
                    return DSFinVKConstants.BusinessCaseTypes.DownPaymentCreation;
                case 0x0089:
                case 0x008A:
                case 0x008B:
                case 0x008C:
                case 0x008D:
                case 0x008E:
                case 0x008F:
                    return DSFinVKConstants.BusinessCaseTypes.DownPaymentReduction;
                case 0x0090:
                    return DSFinVKConstants.BusinessCaseTypes.CashTransferToEmptyTill;
                case 0x0091:
                    return DSFinVKConstants.BusinessCaseTypes.CashTransferToOwner;
                case 0x0092:
                    return DSFinVKConstants.BusinessCaseTypes.CashTransferFromOwner;
                case 0x0093:
                    return DSFinVKConstants.BusinessCaseTypes.CashTransferFromOrToTill;
                case 0x0094:
                    return DSFinVKConstants.BusinessCaseTypes.CashTransferToEmployee;
                case 0x0095:
                    return DSFinVKConstants.BusinessCaseTypes.CashTransferToCashBook;
                case 0x0096:
                    return DSFinVKConstants.BusinessCaseTypes.CashTransferFromCashbook;
                case 0x0097:
                    return DSFinVKConstants.BusinessCaseTypes.CashAmountDifference;
                case 0x00B1:
                case 0x00B2:
                case 0x00B3:
                case 0x00B4:
                case 0x00B5:
                case 0x00B6:
                case 0x00B7:
                    // Ein-/Auszahlung with VAT (e.g. purchase or return of goods paid from/into the till): the sign decides the direction, reversed on a void receipt like pay item 0x0016
                    return (CalculationHelper.GetAmount(chargeItem.ftChargeItemCase, chargeItem.Quantity, chargeItem.Amount) < 0) != (request?.IsVoidReceipt() ?? false) ? DSFinVKConstants.BusinessCaseTypes.CashTransferFromCashbook : DSFinVKConstants.BusinessCaseTypes.CashTransferToCashBook;
                default:
                    return GetDefaultRevenue(logger, chargeItem.ftChargeItemCase);
            }
        }

        private static string GetDefaultRevenue(ILogger logger, long chargeItemCase)
        {
            logger.LogWarning($"ftChargeItemCase {chargeItemCase:x} is not supported. Default BusinessCaseTypes Revenue (Umsatz) used.");
            return DSFinVKConstants.BusinessCaseTypes.Revenue;
        }

    }
}
