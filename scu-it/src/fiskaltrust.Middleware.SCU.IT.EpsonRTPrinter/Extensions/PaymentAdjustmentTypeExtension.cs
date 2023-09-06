using System;
using System.Collections.Generic;
using System.Text;
using fiskaltrust.ifPOS.v1.it;

namespace fiskaltrust.Middleware.SCU.IT.EpsonRTPrinter.Extensions
{
    public static class PaymentAdjustmentTypeExtension
    {
        public static int GetAdjustmentType(this PaymentAdjustmentType paymentAdjustmentType, decimal amount)
        {
            switch (paymentAdjustmentType)
            {
                case PaymentAdjustmentType.Adjustment:
                    return amount < 0 ? 3 : 8;
                case PaymentAdjustmentType.SingleUseVoucher:
                    return 12;
                case PaymentAdjustmentType.FreeOfCharge:
                    return 11;
                case PaymentAdjustmentType.Acconto:
                    return 10;
                default:
                    return 0;
            }
        }
    }
}
