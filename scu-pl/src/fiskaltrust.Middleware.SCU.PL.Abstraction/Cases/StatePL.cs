namespace fiskaltrust.Middleware.SCU.PL.Abstraction.Cases;

/// <summary>
/// PL-localized ftState values. The middle three nibbles (0x0000_0FFF_0000_0000) carry the
/// PL-local flags; the lowest flag nibble bit marks "fiscal device unreachable" — in Poland a
/// non-working register legally means no sale (Art. 111(3) VAT Act), so callers must be able
/// to distinguish this failure from any other error.
/// </summary>
public enum StatePL : ulong
{
    Success = 0x504C_2000_0000_0000,
    Error = 0x504C_2000_EEEE_EEEE,
    DeviceUnreachableError = 0x504C_2001_EEEE_EEEE,

    /// <summary>
    /// The register may have executed the command (e.g. printed the receipt) without confirming it.
    /// Keeps the unreachable flag — no confirmed fiscalization, no sale — and adds its own, because
    /// the remedy differs: check the register's last document before resending, never retry blindly.
    /// </summary>
    DeviceOutcomeUnknownError = 0x504C_2003_EEEE_EEEE,
}
