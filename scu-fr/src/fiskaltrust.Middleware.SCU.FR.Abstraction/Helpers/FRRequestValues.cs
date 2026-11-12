using System.Text.Json;

namespace fiskaltrust.Middleware.SCU.FR.Abstraction.Helpers;

/// <summary>
/// Text values of the free-form request members an SCU signs (<c>cbUser</c>, <c>cbCustomer</c>,
/// <c>ftReceiptCaseData.FR</c>), read the same way whether the SCU runs in the queue's process (typed or Newtonsoft
/// objects) or behind REST (<see cref="JsonElement"/>). Keys match case-insensitively.
/// </summary>
public static class FRRequestValues
{
    /// <summary>The operator: <c>cbUser</c> as a string, or the compact JSON of a structured <c>cbUser</c>; null when absent.</summary>
    public static string? Operator(object? cbUser) => cbUser switch
    {
        null => null,
        string s => NullIfEmpty(s),
        _ => AsJson(cbUser) switch
        {
            null => null,
            { ValueKind: JsonValueKind.String } e => NullIfEmpty(e.GetString()),
            { ValueKind: JsonValueKind.Null or JsonValueKind.Undefined } => null,
            { } e => e.GetRawText(),
        },
    };

    /// <summary>A text property of a free-form object such as <c>cbCustomer</c>; null when absent or not text.</summary>
    public static string? Property(object? value, string name) => AsJson(value) is { ValueKind: JsonValueKind.Object } json ? Text(json, name) : null;

    /// <summary>A text key of the case data's <c>FR</c> block (<c>ftReceiptCaseData.FR.&lt;name&gt;</c>); null when absent.</summary>
    public static string? CaseData(object? caseData, string name)
        => AsJson(caseData) is { ValueKind: JsonValueKind.Object } json
            && TryGet(json, "FR", out var fr) && fr.ValueKind == JsonValueKind.Object
                ? Text(fr, name)
                : null;

    private static string? Text(JsonElement element, string name)
        => TryGet(element, name, out var value) ? value.ValueKind switch
        {
            JsonValueKind.String => NullIfEmpty(value.GetString()),
            JsonValueKind.Number => value.GetRawText(),
            _ => null,
        } : null;

    private static JsonElement? AsJson(object? value)
    {
        try
        {
            return value switch
            {
                null => null,
                JsonElement { ValueKind: JsonValueKind.String } e when LooksLikeJson(e.GetString()) => JsonDocument.Parse(e.GetString()!).RootElement.Clone(),
                JsonElement e => e,
                string s when LooksLikeJson(s) => JsonDocument.Parse(s).RootElement.Clone(),
                string s => JsonSerializer.SerializeToElement(s),
                _ when FRJsonValues.IsJToken(value.GetType()) => JsonDocument.Parse(value.ToString()!).RootElement.Clone(),
                _ => JsonSerializer.SerializeToElement(value, value.GetType()),
            };
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static bool LooksLikeJson(string? text) => text?.TrimStart() is { Length: > 0 } t && t[0] == '{';

    private static bool TryGet(JsonElement element, string name, out JsonElement value)
    {
        foreach (var property in element.EnumerateObject())
        {
            if (string.Equals(property.Name, name, StringComparison.OrdinalIgnoreCase))
            {
                value = property.Value;
                return true;
            }
        }
        value = default;
        return false;
    }

    private static string? NullIfEmpty(string? value) => string.IsNullOrEmpty(value) ? null : value;
}
