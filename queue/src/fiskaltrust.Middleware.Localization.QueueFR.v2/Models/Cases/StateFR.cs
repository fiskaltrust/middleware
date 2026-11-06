namespace fiskaltrust.Middleware.Localization.QueueFR.v2.Models;

/// <summary>
/// FR-localized ftState values (kept in sync with fiskaltrust.Middleware.SCU.FR.Abstraction).
/// A receipt that could not be signed breaks the NF525 chain, so "signing unavailable" carries its
/// own flag in the version nibble: callers and monitoring must be able to tell that failure from
/// any other error.
/// </summary>
public enum StateFR : ulong
{
    Success = 0x4652_2000_0000_0000,
    Error = 0x4652_2000_EEEE_EEEE,
    SigningUnavailableError = 0x4652_2001_EEEE_EEEE,
}
