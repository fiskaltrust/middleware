using fiskaltrust.ifPOS.v1;
using fiskaltrust.Middleware.Contracts.Exceptions;

namespace fiskaltrust.Middleware.Localization.QueueDEFAULT.Extensions
{
    public static class ChargeItemExtensions
    {
        public static bool IsFlagSet(this ChargeItem chargeItem)
        {
            return (chargeItem.ftChargeItemCase & 0x10000) > 0;
        }
    }
}
