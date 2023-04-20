using System;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.ifPOS.v1.it;
using fiskaltrust.Middleware.Contracts.Exceptions;

namespace fiskaltrust.Middleware.Localization.QueueIT.Extensions
{
    public static class PaymentExtensions
    {
        public static PaymentType GetPaymentType(this PayItem payItem)
        {
            switch (payItem.ftPayItemCase & 0xFFFF)
            {
                case 0x0000:
                case 0x0001:
                    return PaymentType.Cash;
                case 0x0003:
                    return PaymentType.Cheque;
                case 0x0004:
                case 0x0005:
                case 0x0009:
                case 0x000A:
                    return PaymentType.CreditCard;
                case 0x0006: //TODO Voucher
                case 0x0002: //TODO foreign currencies
                case 0x0007: //TODO Online payment
                case 0x0008: //TODO  Customer card payment
                case 0x000C: //TODO  SEPA transfer
                case 0x000D: //TODO Other transfer
                case 0x000E: //TODO Cash book expense
                case 0x000F: //TODO Cash book contribution
                case 0x0010: //TODO Levy
                case 0x0011: //TODO Internal/ material consumption Can be used for bill
                case 0x0012: //TODO 0x4954000000000012	Change tip cash
                    throw new NotImplementedException();
                case 0x000B:
                    return PaymentType.NotPaid;
                default:
                    throw new UnknownPayItemException(payItem.ftPayItemCase);
            }
        }
    }
}
