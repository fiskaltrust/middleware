using fiskaltrust.ifPOS.v1;
using fiskaltrust.Middleware.Contracts.Exceptions;

namespace fiskaltrust.Middleware.Localization.QueueDEFAULT.Extensions
{
    /// <summary>
    /// Provides extensions for working with ChargeItem. These extensions should be used to get
    /// information from the ChargeItem, such as which ftChargeItemCase or ftChargeItemCaseFlags are set.
    /// For a more advanced example, refer to <see cref="src/fiskaltrust.Middleware.Localization.QueueIT/Extensions/ChargeItemExtensions.cs"/> in the Italian market folder.
    /// </summary>
    public static class ChargeItemExtensions
    {
        public static bool IsSampleFlagSet(this ChargeItem chargeItem)
        {
            return (chargeItem.ftChargeItemCase & 0x10000) > 0;
        }
    }
}