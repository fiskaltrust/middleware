using System;
using System.Text.Json;
using fiskaltrust.ifPOS.v2;

namespace fiskaltrust.Middleware.SCU.PL.PosNet.Transaction;

/// <summary>
/// The Polish sub-payload of the generic <c>ftReceiptCaseData</c> field — <c>{ "PL": { … } }</c> —
/// which carries everything market-specific a POS asks of the register beyond the fiscal content:
/// the e-receipt customer identifier (<c>eReceipt.customerId</c>) and the printout customization
/// (<c>printout</c>). Keys are matched case-insensitively; the field may arrive as a JSON object or
/// as a JSON string. Data that is not JSON, or has no <c>PL</c> object, is simply not a PL payload —
/// the field is an open extension point shared with other consumers.
/// </summary>
public static class PLReceiptCaseDataReader
{
    /// <summary>The <c>PL</c> object of ftReceiptCaseData, detached from its document, or null.</summary>
    public static JsonElement? ReadPLSection(ReceiptRequest request)
    {
        var json = ToJson(request.ftReceiptCaseData);
        if (json is null)
        {
            return null;
        }

        try
        {
            using var document = JsonDocument.Parse(json);
            if (document.RootElement.ValueKind != JsonValueKind.Object
                || !TryGetPropertyIgnoreCase(document.RootElement, "PL", out var pl)
                || pl.ValueKind != JsonValueKind.Object)
            {
                return null;
            }
            return pl.Clone();
        }
        catch (JsonException)
        {
            return null;
        }
    }

    public static bool TryGetPropertyIgnoreCase(JsonElement element, string name, out JsonElement value)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in element.EnumerateObject())
            {
                if (string.Equals(property.Name, name, StringComparison.OrdinalIgnoreCase))
                {
                    value = property.Value;
                    return true;
                }
            }
        }
        value = default;
        return false;
    }

    private static string? ToJson(object? receiptCaseData) => receiptCaseData switch
    {
        null => null,
        string text => string.IsNullOrWhiteSpace(text) ? null : text,
        JsonElement element => element.ValueKind == JsonValueKind.Null ? null : element.GetRawText(),
        _ => JsonSerializer.Serialize(receiptCaseData),
    };
}
