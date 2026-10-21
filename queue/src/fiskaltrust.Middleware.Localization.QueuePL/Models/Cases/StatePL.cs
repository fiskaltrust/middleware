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
}
