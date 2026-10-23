using System.Text.Json;
using fiskaltrust.ifPOS.v2.pl;

namespace fiskaltrust.Middleware.Localization.QueuePL.Models;

/// <summary>
/// Reads the device state out of the generic <see cref="PLSSCDInfo.InfoData"/> blob. The blob's
/// contract is the <c>PLDeviceInfo</c> model of <c>fiskaltrust.Middleware.SCU.PL.Abstraction</c>;
/// the queue only needs the fiscalization state, so it parses that single property instead of
/// referencing the SCU package.
/// </summary>
internal static class PLSSCDInfoHelper
{
    /// <summary>PLFiscalizationState.NonFiscal in SCU.PL.Abstraction.</summary>
    private const int NonFiscal = 1;

    /// <summary>PLFiscalizationState.Fiscalized in SCU.PL.Abstraction.</summary>
    private const int Fiscalized = 2;

    public static bool IsFiscalized(PLSSCDInfo? info) => ReadFiscalizationState(info) == Fiscalized;

    /// <summary>
    /// True only when the register explicitly reports itself as not fiscalized — an unreadable or
    /// missing state is not the same thing and stays unverified.
    /// </summary>
    public static bool IsNonFiscal(PLSSCDInfo? info) => ReadFiscalizationState(info) == NonFiscal;

    private static int? ReadFiscalizationState(PLSSCDInfo? info)
    {
        if (info?.InfoData is null)
        {
            return null;
        }

        try
        {
            using var document = JsonDocument.Parse(info.InfoData);
            return document.RootElement.TryGetProperty("FiscalizationState", out var state)
                && state.ValueKind == JsonValueKind.Number
                && state.TryGetInt32(out var value)
                    ? value
                    : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
