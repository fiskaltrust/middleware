using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
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
            switch (paymentType)
            {
                case PaymentType.Cash:
                default:
                    return new EpsonPaymentType() { PaymentType = 0, Index = 0 };
                case PaymentType.Cheque:
                    return new EpsonPaymentType() { PaymentType = 1, Index = 0 };
                case PaymentType.CreditCard:
                    return new EpsonPaymentType() { PaymentType = 2, Index = 0 };
                case PaymentType.Voucher:
                    return new EpsonPaymentType() { PaymentType = 6, Index = 1 };
                case PaymentType.NotPaid:
                    return new EpsonPaymentType() { PaymentType = 5, Index = 0 };
            }
        }
    }
}