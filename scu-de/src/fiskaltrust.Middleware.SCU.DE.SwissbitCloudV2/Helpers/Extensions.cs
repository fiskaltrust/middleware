using System;
using fiskaltrust.ifPOS.v1.de;

namespace fiskaltrust.Middleware.SCU.DE.SwissbitCloudV2.Helpers
{
    internal static class Extensions
    {
        public static TseState ToTseState(this SwissbitCloudV2TseState value)
        {
            return value switch
            {
                SwissbitCloudV2TseState.initialized => new TseState { CurrentState = TseStates.Initialized },
                SwissbitCloudV2TseState.disabled => new TseState { CurrentState = TseStates.Terminated },
                _ => new TseState { CurrentState = TseStates.Uninitialized },
            };
        }

        public static TseStates ToTseStateEnum(this SwissbitCloudV2TseState value)
        {
            return value switch
            {
                SwissbitCloudV2TseState.initialized => TseStates.Initialized,
                SwissbitCloudV2TseState.disabled => TseStates.Terminated,
                _ => TseStates.Uninitialized
            };
        }

        public static SwissbitCloudV2TseState ToSwissbitTseState(this TseStates value)
        {
            return value switch
            {
                TseStates.Initialized => SwissbitCloudV2TseState.initialized,
                TseStates.Terminated => SwissbitCloudV2TseState.disabled,
                _ => SwissbitCloudV2TseState.uninitialized,
            };
        }

        public static string AsBase64(this string value) => Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(value));
    }
}
