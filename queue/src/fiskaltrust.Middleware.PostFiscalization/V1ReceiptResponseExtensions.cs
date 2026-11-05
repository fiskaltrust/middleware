using System.Text.Json;
using V1 = fiskaltrust.ifPOS.v1;

namespace fiskaltrust.Middleware.PostFiscalization;

/// <summary>
/// The v1 flavour of <c>ReceiptResponse.IsFiscalized()</c> (RFC 712) for the legacy stack, where <c>ftStateData</c> is a
/// string. True when <c>ftState</c> is not an error state, or when the persisted <c>PostFiscalization.FiscalizationSucceeded</c>
/// is true: a receipt whose eInvoicing/eReporting finalize call failed carries the error state although the fiscal
/// record exists. Every legacy lookup that needs to know whether a persisted receipt is fiscalized must use this
/// predicate instead of testing <c>ftState</c> directly.
/// </summary>
public static class V1ReceiptResponseExtensions
{
    /// <summary>
    /// The error state in the low 32 bits of a v1 <c>ftState</c>, tested as a mask: the fail state (0xFFFF_FFFF) matches
    /// it as well, which is what the legacy lookups always tested.
    /// </summary>
    public const long ErrorStateMask = 0xEEEE_EEEE;

    public static bool IsFiscalized(this V1.ReceiptResponse receiptResponse)
        => (receiptResponse.ftState & ErrorStateMask) != ErrorStateMask || HasFiscalizationSucceededMarker(receiptResponse.ftStateData);

    /// <summary>Whether a v1 <c>ftStateData</c> string carries <c>PostFiscalization.FiscalizationSucceeded = true</c>; anything that is not JSON with that marker counts as no marker.</summary>
    public static bool HasFiscalizationSucceededMarker(string? stateData)
    {
        if (string.IsNullOrWhiteSpace(stateData))
        {
            return false;
        }

        try
        {
            using var document = JsonDocument.Parse(stateData);
            var root = document.RootElement;
            if (root.ValueKind == JsonValueKind.String)
            {
                // The state data was serialized into a JSON string once more on its way through a v1 store.
                return HasFiscalizationSucceededMarker(root.GetString());
            }

            return root.ValueKind == JsonValueKind.Object
                && root.TryGetProperty("PostFiscalization", out var section) && section.ValueKind == JsonValueKind.Object
                && section.TryGetProperty("FiscalizationSucceeded", out var succeeded) && succeeded.ValueKind == JsonValueKind.True;
        }
        catch (JsonException)
        {
            return false;
        }
    }
}
