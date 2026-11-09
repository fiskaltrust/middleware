namespace fiskaltrust.Middleware.SCU.PL.Abstraction.Cases;

/// <summary>
/// PL-localized ftState values. Every failure — an unreachable register included — is a plain
/// error for now; PL-local error flags are deliberately left out until it is clear which ones
/// callers need.
/// </summary>
public enum StatePL : ulong
{
    Success = 0x504C_2000_0000_0000,
    Error = 0x504C_2000_EEEE_EEEE,
}
