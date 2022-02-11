using fiskaltrust.ifPOS.v1.de;
using Newtonsoft.Json;

namespace fiskaltrust.Middleware.SCU.DE.DeutscheFiskal.Models
{
    public enum KeyState
    {
        [JsonProperty("ACTIVE")]
        Active,
        [JsonProperty("NONACTIVE")]
        NonActive
    }

    public static class KeyStateExtensions
    {
        public static TseStates ToTseState(this KeyState value)
        {
            return value switch
            {
                KeyState.Active => TseStates.Initialized,
                KeyState.NonActive => TseStates.Terminated,
                _ => TseStates.Uninitialized
            };
        }
    }
}
