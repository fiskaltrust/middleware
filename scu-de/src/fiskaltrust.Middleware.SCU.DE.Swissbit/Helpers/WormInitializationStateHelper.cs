using System;
using System.Collections.Generic;
using System.Text;
using fiskaltrust.ifPOS.v1.de;
using static fiskaltrust.Middleware.SCU.DE.Swissbit.Interop.NativeFunctionPointer;

namespace fiskaltrust.Middleware.SCU.DE.Swissbit.Helpers
{
    public static class WormInitializationStateHelper
    {
        public static TseStates ToTseStates(this WormInitializationState initializationState)
        {
            return initializationState switch
            {
                WormInitializationState.WORM_INIT_INITIALIZED => TseStates.Initialized,
                WormInitializationState.WORM_INIT_DECOMMISSIONED => TseStates.Terminated,
                _ => TseStates.Uninitialized,
            };
        }
    }
}
