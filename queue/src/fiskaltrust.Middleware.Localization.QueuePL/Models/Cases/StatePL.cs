namespace fiskaltrust.Middleware.Localization.QueuePL.Models;

/// <summary>
/// PL-localized ftState values (kept in sync with fiskaltrust.Middleware.SCU.PL.Abstraction).
/// The lowest local-flag nibble marks "fiscal register unreachable" — in Poland no working
/// register legally means no sale (Art. 111(3) VAT Act), so callers and monitoring must be able
/// to distinguish this failure from any other error.
/// </summary>
public enum StatePL : ulong
{
    Error = 0x504C_2000_EEEE_EEEE,
    DeviceUnreachableError = 0x504C_2001_EEEE_EEEE,

    /// <summary>
    /// The register may have executed the command (e.g. printed the receipt) without confirming it.
    /// Keeps the unreachable flag and adds its own: check the register before resending.
    /// </summary>
    DeviceOutcomeUnknownError = 0x504C_2003_EEEE_EEEE,
}
