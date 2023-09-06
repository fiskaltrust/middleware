using fiskaltrust.ifPOS.v1.it;

namespace fiskaltrust.Middleware.SCU.IT.EpsonRTPrinter.Extensions
{
    public static class PaymentAdjustmentTypeExtension
    {
        public static int GetAdjustmentType(this PaymentAdjustmentType paymentAdjustmentType, decimal amount)
        {
            return paymentAdjustmentType switch
            {
                PaymentAdjustmentType.Adjustment => amount < 0 ? 3 : 8,
                PaymentAdjustmentType.SingleUseVoucher => 12,
                PaymentAdjustmentType.FreeOfCharge => 11,
                PaymentAdjustmentType.Acconto => 10,
                _ => 0,
            };
        }
    }
}
