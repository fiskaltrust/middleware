namespace fiskaltrust.Middleware.SCU.FR.Abstraction.Cases;

/// <summary>
/// FR-localized ftState values. In France fiscalization is a software act (art. 286-I-3° bis CGI,
/// NF525): a receipt that could not be signed breaks the chain, so "signing unavailable" gets its
/// own local flag in the version nibble to keep it distinguishable from any other error.
/// </summary>
public enum StateFR : ulong
{
    Success = 0x4652_2000_0000_0000,
    Error = 0x4652_2000_EEEE_EEEE,
    SigningUnavailableError = 0x4652_2001_EEEE_EEEE,
}
