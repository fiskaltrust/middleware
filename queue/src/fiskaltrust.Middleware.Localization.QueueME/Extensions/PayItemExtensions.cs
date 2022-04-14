using System.Linq;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.ifPOS.v2.me;
using fiskaltrust.Middleware.Localization.QueueME.Exceptions;

namespace fiskaltrust.Middleware.Localization.QueueME.Extensions
{
#pragma warning disable
    public static class PayItemExtensions
    {
        private static readonly long[] _cashLocalPayItemCases = { 0x0000, 0x0001, 0x0005, 0x0010};
        private static readonly long[] _nonCashPayItemCases = { 0x0002, 0x0003, 0x0004, 0x0005, 0x0006, 0x0007, 0x0008, 0x0009 };


        public static PaymentMethodTypeSType GetPaymentMethodType(this PayItem item)
        {
            switch (item.ftPayItemCase & 0xFFFF)
            {
                case 0x0000:
                    return PaymentMethodTypeSType.BANKNOTE;
                case 0x0001:
                    return PaymentMethodTypeSType.CARD;
                case 0x0002:
                    return PaymentMethodTypeSType.BUSINESSCARD;
                case 0x0003:
                    return PaymentMethodTypeSType.SVOUCHER;
                case 0x0004:
                    return PaymentMethodTypeSType.COMPANY;
                case 0x0005:
                    return PaymentMethodTypeSType.ORDER;
                case 0x0006:
                    return PaymentMethodTypeSType.ADVANCE;
                case 0x0007:
                    return PaymentMethodTypeSType.ACCOUNT;
                case 0x0008:
                    return PaymentMethodTypeSType.FACTORING;
                case 0x0009:
                    return PaymentMethodTypeSType.OTHER;
                case 0x0010:
                    return PaymentMethodTypeSType.OTHERCASH;
                default:
                    throw new UnknownPaymentMethodeTypeException($"PayItemCase {item.ftPayItemCase} incorrect!");

            }
        }

        public static bool IsCashLocalCurrency(this PayItem item)
        {
            var payItemCase = item.ftPayItemCase & 0xFFFF;
            return _cashLocalPayItemCases.Any(x => x == payItemCase);
        }

        public static bool IsNonCashLocalCurrency(this PayItem item)
        {
            var payItemCase = item.ftPayItemCase & 0xFFFF;
            return _nonCashPayItemCases.Any(x => x == payItemCase);
        }


    }
}
