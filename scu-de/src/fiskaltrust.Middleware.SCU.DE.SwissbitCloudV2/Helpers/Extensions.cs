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
                SwissbitCloudV2TseState.INITIALIZED => new TseState { CurrentState = TseStates.Initialized },
                SwissbitCloudV2TseState.DISABLED => new TseState { CurrentState = TseStates.Terminated },
                _ => new TseState { CurrentState = TseStates.Uninitialized },
            };
        }

        public static TseStates ToTseStateEnum(this SwissbitCloudV2TseState value)
        {
            return value switch
            {
                SwissbitCloudV2TseState.INITIALIZED => TseStates.Initialized,
                SwissbitCloudV2TseState.DISABLED => TseStates.Terminated,
                _ => TseStates.Uninitialized
            };
        }

        public static SwissbitCloudV2TseState ToSwissbitTseState(this TseStates value)
        {
            return value switch
            {
                TseStates.Initialized => SwissbitCloudV2TseState.INITIALIZED,
                TseStates.Terminated => SwissbitCloudV2TseState.DISABLED,
                _ => SwissbitCloudV2TseState.UNINITIALIZED,
            };
        }

        public static string AsBase64(this string value) => Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(value));
    }
}
