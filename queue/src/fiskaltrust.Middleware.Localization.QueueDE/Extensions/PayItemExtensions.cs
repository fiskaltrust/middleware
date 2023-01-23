using System.Linq;
using fiskaltrust.ifPOS.v1;

namespace fiskaltrust.Middleware.Localization.QueueDE.Extensions
{
#pragma warning disable
    public static class PayItemExtensions
    {
        private static readonly long[] _cashLocalPayItemCases = { 0x0000, 0x0001, 0x000B };
        private static readonly long[] _cashForeignPayItemCases = { 0x0002, 0x000C };
        private static readonly long[] _nonCashPayItemCases = { 0x0003, 0x0004, 0x0005, 0x0006, 0x0007, 0x0008, 0x0009 };
        private static readonly long[] _nonCashForeignPayItemCases = { };

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

        public static bool IsCashForeignCurrency(this PayItem item)
        {
            var payItemCase = item.ftPayItemCase & 0xFFFF;
            return _cashForeignPayItemCases.Any(x => x == payItemCase) && GetCurrency(item) == null;
        }

        public static bool IsNonCashForeignCurrency(this PayItem item)
        {
            var payItemCase = item.ftPayItemCase & 0xFFFF;
            return _nonCashForeignPayItemCases.Any(x => x == payItemCase) && GetCurrency(item) != null;
        }

        public static string GetCurrency(this PayItem item)
        {
            // TODO: Extract currency from PayItemCaseData
            return "???";
        }

        public static bool IsTillPayment(this PayItem payItem)
        {
            return (payItem.ftPayItemCase & 0xFFFF) switch
            {
                0x0012 or
                0x0013 or
                0x0014 or
                0x0015 or
                0x0016 or
                0x0017
                => true,
                _ => false
            };
        }

        public static bool IsCashPaymentType(this PayItem payItem)
        {
            return (payItem.ftPayItemCase & 0xFFFF) switch
            {
                0x0000 or
                0x0001 or
                0x0002 or
                0x000B or
                0x000C
                => true,
                _ => false
            };
        }
    }
}
