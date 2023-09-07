using fiskaltrust.ifPOS.v1;
using fiskaltrust.ifPOS.v1.it;

namespace fiskaltrust.Middleware.SCU.IT.EpsonRTPrinter.Extensions
{
    public static class ChargeItemV2Extensions

    {
        // TODO: check VAT rate table on printer at the moment according to xml example
        private static readonly int _vatRateBasic = 1;
        private static readonly int _vatRateDeduction1 = 2;
        private static readonly int _vatRateDeduction2 = 3;
        private static readonly int _vatRateDeduction3 = 4;
        private static readonly int _vatRateZero = 0;
        private static readonly int _vatRateUnknown = -1;
        private static readonly int _notTaxable = 0;

        public static PaymentAdjustmentType GetV2PaymentAdjustmentType(this ChargeItem chargeItem)
        {
            return (chargeItem.ftChargeItemCase & 0xFFFF) switch
            {
                long i when i >= 0x0023 && i <= 0x0027 => PaymentAdjustmentType.Adjustment,
                long i when i >= 0x0028 && i <= 0x002C => PaymentAdjustmentType.SingleUseVoucher,
                _ => PaymentAdjustmentType.Adjustment,
            };
        }

        public static bool IsV2PaymentAdjustment(this ChargeItem chargeItem)
        {
            return (chargeItem.ftChargeItemCase & 0xFFFF) switch
            {
                long i when i >= 0x0023 && i <= 0x0027 => true,
                long i when i >= 0x0028 && i <= 0x002D && chargeItem.GetAmount() < 0 => true,
                _ => false,
            };
        }

        public static bool IsV2MultiUseVoucherRedeem(this ChargeItem chargeItem)
        {
            return (chargeItem.ftChargeItemCase & 0xFFFF) switch
            {
                0x002D => true && chargeItem.GetAmount() < 0,
                _ => false,
            };
        }

        public static bool IsV2MultiUseVoucherSale(this ChargeItem chargeItem)
        {
            return (chargeItem.ftChargeItemCase & 0xFFFF) switch
            {
                0x002D => true && chargeItem.GetAmount() > 0,
                _ => false,
            };
        }

        public static int GetVatGroup(this ChargeItem chargeItem)
        {
            return (chargeItem.ftChargeItemCase & 0xF) switch
            {
                0x0 => _vatRateUnknown,
                0x1 => _vatRateDeduction1,
                0x2 => _vatRateDeduction2,
                0x3 => _vatRateBasic,
                0x4 => _vatRateDeduction3,
                0x5 => throw new System.Exception("Currently not supported"),
                0x6 => throw new System.Exception("Currently not supported"),
                0x7 => _vatRateZero,
                0x8 => _notTaxable,
                _ => _vatRateUnknown,
            };
        }

        public static decimal GetAmount(this ChargeItem chargeItem) => chargeItem.Quantity < 0 && chargeItem.Amount >= 0 ? chargeItem.Amount * -1 : chargeItem.Amount;
    }
}
