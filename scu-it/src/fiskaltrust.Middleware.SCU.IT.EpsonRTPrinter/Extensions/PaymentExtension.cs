using fiskaltrust.ifPOS.v1.it;

namespace fiskaltrust.Middleware.SCU.IT.EpsonRTPrinter.Extensions
{
    public struct EpsonPaymentType
    {
        public int PaymentType;
        public int Index;
    }

    public static class PaymentExtension
    {
        public static EpsonPaymentType GetEpsonPaymentType(this PaymentType paymentType)
        {
            return paymentType switch
            {
                PaymentType.Cheque => new EpsonPaymentType() { PaymentType = 1, Index = 0 },
                PaymentType.CreditCard => new EpsonPaymentType() { PaymentType = 2, Index = 1 },
                PaymentType.Ticket => new EpsonPaymentType() { PaymentType = 3, Index = 0 },
                PaymentType.MultipleTickets => new EpsonPaymentType() { PaymentType = 4, Index = 0 },
                PaymentType.NotPaid => new EpsonPaymentType() { PaymentType = 5, Index = 0 },
                PaymentType.Voucher => new EpsonPaymentType() { PaymentType = 6, Index = 1 },
                PaymentType.PaymentDiscount => new EpsonPaymentType() { PaymentType = 6, Index = 0 },
                _ => new EpsonPaymentType() { PaymentType = 0, Index = 0 },
            };
        }
    }
}