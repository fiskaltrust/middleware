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

        public static decimal InverseAmountIfNotVoidReceipt(this PayItem payItem, bool isVoidReceipt) => isVoidReceipt ? payItem.Amount : payItem.Amount * -1;

        public static bool IsPositionCancellation(this PayItem payItem) => (payItem.ftPayItemCase & 0x0000_0000_0020_0000) > 0x0000;

    }
}
