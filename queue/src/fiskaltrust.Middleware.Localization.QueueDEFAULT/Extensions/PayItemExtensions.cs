using System;
using fiskaltrust.ifPOS.v1;

namespace fiskaltrust.Middleware.Localization.QueueDEFAULT.Extensions
{
    /// <summary>
    /// Provides extensions for working with PayItem. These extensions should be used to get
    /// information from the PayItem, such as which ftPayItemCase flags are set.
    /// For a more advanced example, refer to <see cref="fiskaltrust.Middleware.Localization.QueueIT.Extensions.PayItemExtensions"/> in the Italian market folder.
    /// </summary>
    public static class PayItemExtensions
    {
        public static bool IsSampleFlagSet(this PayItem payItem)
        {
            return (payItem.ftPayItemCase & 0x10000) > 0;
        }
    }
}