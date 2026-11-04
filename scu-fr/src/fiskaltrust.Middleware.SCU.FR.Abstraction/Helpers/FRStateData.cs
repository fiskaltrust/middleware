using System.Text.Json;
using fiskaltrust.ifPOS.v2;

namespace fiskaltrust.Middleware.SCU.FR.Abstraction.Helpers;

/// <summary>
/// Reads what the French queue hands the SCU through the response's <c>ftStateData</c> JSON - its
/// <c>FR</c> block - as the ES queue does, so the SCU contract carries no market data of its own.
/// The SCU may run in the queue's process (the queue's typed object) or behind REST (a
/// <see cref="JsonElement"/>), so it reads the JSON form in both cases.
/// </summary>
public static class FRStateData
{
    public const string Block = "FR";
    public const string CopyNumberProperty = "CopyNumber";

    /// <summary>
    /// The reprint number of a copy (0x3010), 1 for the first copy of the copied receipt (R13
    /// ENC-DUP-PRN-NUM, market-fr#235); null on every other receipt and on a training copy.
    /// </summary>
    public static int? CopyNumber(ReceiptResponse response)
    {
        var stateData = AsJson(response.ftStateData);
        return stateData is { ValueKind: JsonValueKind.Object } json
            && json.TryGetProperty(Block, out var fr) && fr.ValueKind == JsonValueKind.Object
            && fr.TryGetProperty(CopyNumberProperty, out var number) && number.ValueKind == JsonValueKind.Number
                ? number.GetInt32()
                : null;
    }

    private static JsonElement? AsJson(object? stateData) => stateData switch
    {
        null => null,
        JsonElement json => json,
        string text => JsonDocument.Parse(text).RootElement,
        _ => JsonSerializer.SerializeToElement(stateData, stateData.GetType()),
    };
}
