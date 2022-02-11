using System;
using fiskaltrust.ifPOS.v1.de;
using fiskaltrust.Middleware.SCU.DE.FiskalyCertified.Helpers;

namespace fiskaltrust.Middleware.SCU.DE.FiskalyCertified.Helpers
{
    internal static class Extensions
    {
        public static TseState ToTseState(this FiskalyTseState value)
        {
            return value switch
            {
                FiskalyTseState.INITIALIZED => new TseState { CurrentState = TseStates.Initialized },
                FiskalyTseState.DISABLED => new TseState { CurrentState = TseStates.Terminated },
                _ => new TseState { CurrentState = TseStates.Uninitialized },
            };
        }

        public static TseStates ToTseStateEnum(this FiskalyTseState value)
        {
            return value switch
            {
                FiskalyTseState.INITIALIZED => TseStates.Initialized,
                FiskalyTseState.DISABLED => TseStates.Terminated,
                _ => TseStates.Uninitialized
            };
        }

        public static FiskalyTseState ToFiskalyTseState(this TseStates value)
        {
            return value switch
            {
                TseStates.Initialized => FiskalyTseState.INITIALIZED,
                TseStates.Terminated => FiskalyTseState.DISABLED,
                _ => FiskalyTseState.UNINITIALIZED,
            };
        }

        public static string AsBase64(this string value) => Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(value));
    }
}
