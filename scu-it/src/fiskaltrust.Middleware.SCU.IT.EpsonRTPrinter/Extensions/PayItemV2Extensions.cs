using fiskaltrust.ifPOS.v1;
using fiskaltrust.ifPOS.v1.it;

namespace fiskaltrust.Middleware.SCU.IT.EpsonRTPrinter.Extensions
{
    public static class PayItemV2Extensions
    {
        public static PaymentType GetV2PaymentType(this PayItem payItem)
        {
            return (payItem.ftPayItemCase & 0xFF) switch
            {
                0x00 => PaymentType.Cash,
                0x01 => PaymentType.Cash,
                0x02 => PaymentType.Cash,
                0x03 => PaymentType.Cheque,
                0x04 => PaymentType.CreditCard,
                0x05 => PaymentType.CreditCard,
                0x06 => PaymentType.Voucher,
                0x07 => PaymentType.NotPaid,
                0x08 => PaymentType.NotPaid,
                0x09 => PaymentType.NotPaid,
                0x0A => PaymentType.CreditCard,
                0x0B => PaymentType.CreditCard,
                0x0C => PaymentType.Cash,
                0x0D => PaymentType.NotPaid,
                0x0E => PaymentType.NotPaid,
                _ => PaymentType.Cash,
            };
        }

        public static bool IsV2VoucherRedeem(this PayItem payItem) => payItem.GetV2PaymentType() == PaymentType.Voucher && payItem.GetAmount() > 0;

        public static bool IsV2VoucherSale(this PayItem payItem) => payItem.GetV2PaymentType() == PaymentType.Voucher && payItem.GetAmount() < 0;

        public static decimal GetAmount(this PayItem payItem) => payItem.Quantity < 0 && payItem.Amount >= 0 ? payItem.Amount * -1 : payItem.Amount;
    }
}
