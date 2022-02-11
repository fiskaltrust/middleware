using fiskaltrust.ifPOS.v1.de;
using fiskaltrust.Middleware.SCU.DE.CryptoVision.Models;

namespace fiskaltrust.Middleware.SCU.DE.CryptoVision.Helpers
{
    public static class LifeCycleStateHelper
    {
        public static TseStates ToTseStates(this SeLifeCycleState lifeCycleState)
        {
            //TODO review NoTime, Disabled, Deactivated

            return lifeCycleState switch
            {
                SeLifeCycleState.lcsNotInitialized => TseStates.Uninitialized,
                SeLifeCycleState.lcsNoTime => TseStates.Initialized,
                SeLifeCycleState.lcsActive => TseStates.Initialized,
                SeLifeCycleState.lcsDeactivated => TseStates.Terminated,
                SeLifeCycleState.lcsDisabled => TseStates.Terminated,
                _ => TseStates.Uninitialized,
            };
        }

    }
}
