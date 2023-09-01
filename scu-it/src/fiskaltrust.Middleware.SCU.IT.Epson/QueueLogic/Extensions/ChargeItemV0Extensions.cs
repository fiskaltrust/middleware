using fiskaltrust.ifPOS.v1;
using fiskaltrust.ifPOS.v1.it;

namespace fiskaltrust.Middleware.SCU.IT.Epson.QueueLogic.Extensions
{

    public static class ChargeItemV0Extensions
    {

        // TODO: check VAT rate table on printer at the moment according to xml example
        private static readonly int _vatRateBasic = 1;
        private static readonly int _vatRateDeduction1 = 2;
        private static readonly int _vatRateDeduction2 = 3;
        private static readonly int _vatRateDeduction3 = 4;
        private static readonly int _vatRateZero = 0;
        private static readonly int _vatRateUnknown = -1;

        public static PaymentAdjustmentType? GetV0PaymentAdjustmentType(this ChargeItem chargeItem)
        {
            return (chargeItem.ftChargeItemCase & 0xFFFF) switch
            {
                long i when i >= 0x0023 && i <= 0x0027 => PaymentAdjustmentType.Adjustment,
                long i when i >= 0x0028 && i <= 0x002C => PaymentAdjustmentType.SingleUseVoucher,
                _ => null,
            };
        }

        public static bool IsV0PaymentAdjustment(this ChargeItem chargeItem)
        {
            return (chargeItem.ftChargeItemCase & 0xFFFF) switch
            {
                long i when i >= 0x0023 && i <= 0x0027 => true,
                long i when i >= 0x0028 && i <= 0x002D && chargeItem.GetAmount() < 0 => true,
                _ => false,
            };
        }

        public static bool IsV0MultiUseVoucherRedeem(this ChargeItem chargeItem)
        {
            return (chargeItem.ftChargeItemCase & 0xFFFF) switch
            {
                0x002D => true && chargeItem.GetAmount() < 0,
                _ => false,
            };
        }

        public static bool IsV0MultiUseVoucherSale(this ChargeItem chargeItem)
        {
            return (chargeItem.ftChargeItemCase & 0xFFFF) switch
            {
                0x002D => true && chargeItem.GetAmount() > 0,
                _ => false,
            };
        }

        public static int GetV0VatGroup(this ChargeItem chargeItem)
        {
            // TODO: check VAT rate table on printer at the moment according to xml example
            switch (chargeItem.ftChargeItemCase & 0xFFFF)
            {
                //0
                case 0x0005:
                case 0x000C:
                case 0x0011:
                case 0x0016:
                case 0x001B:
                case 0x0020:
                case 0x0027:
                case 0x002C:
                case 0x002D:
                    return _vatRateZero;
                //22
                case 0x0001:
                case 0x0008:
                case 0x000D:
                case 0x0012:
                case 0x0017:
                case 0x001C:
                case 0x0023:
                case 0x0028:
                    return _vatRateBasic;
                //10
                case 0x0002:
                case 0x0009:
                case 0x000E:
                case 0x0013:
                case 0x0018:
                case 0x001D:
                case 0x0024:
                case 0x0029:
                    return _vatRateDeduction1;
                //5
                case 0x0003:
                case 0x000A:
                case 0x000F:
                case 0x0014:
                case 0x0019:
                case 0x001E:
                case 0x0025:
                case 0x002A:
                    return _vatRateDeduction2;
                //4
                case 0x0004:
                case 0x000B:
                case 0x0010:
                case 0x0015:
                case 0x001A:
                case 0x001F:
                case 0x0026:
                case 0x002B:
                    return _vatRateDeduction3;
                default:
                    return _vatRateUnknown;
            }
        }
        public static decimal GetAmount(this ChargeItem chargeItem) => chargeItem.Quantity < 0 && chargeItem.Amount >= 0 ? chargeItem.Amount * -1 : chargeItem.Amount;
    }
}
