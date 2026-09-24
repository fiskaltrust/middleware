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
    /// <summary>PLFiscalizationState.Fiscalized in SCU.PL.Abstraction.</summary>
    private const int Fiscalized = 2;

    public static bool IsFiscalized(PLSSCDInfo? info)
    {
        if (info?.InfoData is null)
        {
            return false;
        }

        try
        {
            using var document = JsonDocument.Parse(info.InfoData);
            return document.RootElement.TryGetProperty("FiscalizationState", out var state)
                && state.ValueKind == JsonValueKind.Number
                && state.GetInt32() == Fiscalized;
        }
        catch (JsonException)
        {
            return false;
        }
    }
}
