using System.Text.Json;
using fiskaltrust.ifPOS.v2.fr;

namespace fiskaltrust.Middleware.Localization.QueueFR.v2.Models;

/// <summary>
/// Reads the SCU state out of the generic <see cref="FRSSCDInfo.InfoData"/> blob. The blob's
/// contract is owned by the SCU implementation; the queue only needs to know whether usable
/// signature creation data is present and which body certified it, so it parses those two
/// properties instead of referencing an SCU package.
/// </summary>
internal static class FRSSCDInfoHelper
{
    public static bool HasSignatureCreationData(FRSSCDInfo? info) => TryRead(info, "SignatureCreationDataAvailable", out var element)
        && element.ValueKind is JsonValueKind.True;

    public static string? GetCertificationBody(FRSSCDInfo? info) => TryRead(info, "CertificationBody", out var element) && element.ValueKind == JsonValueKind.String
        ? element.GetString()
        : null;

    private static bool TryRead(FRSSCDInfo? info, string property, out JsonElement element)
    {
        element = default;
        if (info?.InfoData is null)
        {
            return false;
        }

        try
        {
            using var document = JsonDocument.Parse(info.InfoData);
            if (!document.RootElement.TryGetProperty(property, out var found))
            {
                return false;
            }

            // The document is disposed on return, so hand out a detached copy.
            element = found.Clone();
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }
}
