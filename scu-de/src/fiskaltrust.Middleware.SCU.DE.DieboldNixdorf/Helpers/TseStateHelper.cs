using fiskaltrust.ifPOS.v1.de;
using fiskaltrust.Middleware.SCU.DE.DieboldNixdorf.Models;
using fiskaltrust.Middleware.SCU.DE.DieboldNixdorf.Models.Enums;

namespace fiskaltrust.Middleware.SCU.DE.DieboldNixdorf.Helpers
{
    public static class TseStateHelper
    {
        public static TseStates ToTseStates(this SlotInfo slotInfo)
        {
            if ((slotInfo.TseStatus & SlotTseStates.Initialized) == SlotTseStates.Initialized)
            {
                return TseStates.Initialized;
            }
            else if ((slotInfo.TseStatus & SlotTseStates.Disabled) == SlotTseStates.Disabled)
            {
                return TseStates.Terminated;
            }
            else
            {
                return TseStates.Uninitialized;
            }
        }
    }
}