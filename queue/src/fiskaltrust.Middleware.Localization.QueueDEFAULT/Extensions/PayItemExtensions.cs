using System;
using fiskaltrust.ifPOS.v1;

namespace fiskaltrust.Middleware.Localization.QueueDEFAULT.Extensions
{
    public static class PayItemExtensions
    {
        public static bool IsFlagSet(this PayItem payItem)
        {
            return (payItem.ftPayItemCase & 0x10000) > 0;
        }
    }
}
