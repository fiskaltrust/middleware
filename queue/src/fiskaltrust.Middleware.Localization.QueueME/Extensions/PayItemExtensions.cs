using System.Linq;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.ifPOS.v1.me;
using fiskaltrust.Middleware.Localization.QueueME.Exceptions;

namespace fiskaltrust.Middleware.Localization.QueueME.Extensions
{
#pragma warning disable
    public static class PayItemExtensions
    {
        private static readonly long[] _cashLocalPayItemCases = { 0x0000, 0x0001, 0x0005, 0x0010 };
        private static readonly long[] _nonCashPayItemCases = { 0x0002, 0x0003, 0x0004, 0x0005, 0x0006, 0x0007, 0x0008, 0x0009 };

        public static PaymentType GetPaymentMethodType(this PayItem item)
        {
            switch (item.ftPayItemCase & 0xFFFF)
            {
                case 0x0000:
                case 0x0001:
                case 0x0002:
                case 0x000D:
                case 0x000E:
                    return PaymentType.Banknote;
                case 0x0003:
                case 0x0008:
                case 0x000A:
                case 0x000B:
                case 0x0015:
                    return PaymentType.OtherNonCash;
                case 0x0004:
                case 0x0005:
                    return PaymentType.Card;
                case 0x0006:
                case 0x0007:
                    return PaymentType.BusinessCard;
                case 0x0009:
                    return PaymentType.Company;
                case 0x000C:
                    return PaymentType.OtherCash;
                case 0x000F:
                    return PaymentType.Voucher;
                case 0x0010:
                case 0x0011:
                    return PaymentType.Order;
                case 0x0012:
                    return PaymentType.Advance;
                case 0x0013:
                    return PaymentType.Account;
                case 0x0014:
                    return PaymentType.Factoring;
                case 0x0016:
                    return PaymentType.OtherCash;
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
