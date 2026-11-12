using System;
using System.Text.Json;
using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.fr;

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
    public const string ArchiveProperty = "Archive";
    public const string MigrationProperty = "Migration";

    /// <summary>
    /// The reprint number of a copy (0x3010), 1 for the first copy of the copied receipt (R13
    /// ENC-DUP-PRN-NUM, market-fr#235); null on every other receipt and on a training copy.
    /// </summary>
    public static int? CopyNumber(ReceiptResponse response)
    {
        var stateData = AsJson(response.ftStateData);
        return stateData is { ValueKind: JsonValueKind.Object } json
            && TryGet(json, Block, out var fr) && fr.ValueKind == JsonValueKind.Object
            && TryGet(fr, CopyNumberProperty, out var number) && number.ValueKind == JsonValueKind.Number
                ? number.GetInt32()
                : null;
    }

    /// <summary>
    /// The boundaries an archive receipt (0x3011) signs (market-fr#221); null on every other receipt
    /// and on a training archive receipt.
    /// </summary>
    public static FRArchiveBoundaries? Archive(ReceiptResponse response)
    {
        var stateData = AsJson(response.ftStateData);
        return stateData is { ValueKind: JsonValueKind.Object } json
            && TryGet(json, Block, out var fr) && fr.ValueKind == JsonValueKind.Object
            && TryGet(fr, ArchiveProperty, out var archive) && archive.ValueKind == JsonValueKind.Object
                ? archive.Deserialize<FRArchiveBoundaries>(ReadOptions)
                : null;
    }

    /// <summary>The migration event a finish-migration receipt (0x4022) signs (market-fr ADR 0003); null on every other receipt.</summary>
    public static FRMigrationEvent? Migration(ReceiptResponse response)
    {
        var stateData = AsJson(response.ftStateData);
        return stateData is { ValueKind: JsonValueKind.Object } json
            && TryGet(json, Block, out var fr) && fr.ValueKind == JsonValueKind.Object
            && TryGet(fr, MigrationProperty, out var migration) && migration.ValueKind == JsonValueKind.Object
                ? migration.Deserialize<FRMigrationEvent>(ReadOptions)
                : null;
    }

    // The queue writes its state data as typed objects; behind REST the casing may differ.
    private static readonly JsonSerializerOptions ReadOptions = new() { PropertyNameCaseInsensitive = true };

    private static JsonElement? AsJson(object? stateData) => stateData switch
    {
        null => null,
        JsonElement json => json,
        string text => JsonDocument.Parse(text).RootElement,
        // A Newtonsoft object is read through its own JSON text: System.Text.Json would walk its Parent/Root cycle.
        _ when FRJsonValues.IsJToken(stateData.GetType()) => JsonDocument.Parse(stateData.ToString()!).RootElement,
        _ => JsonSerializer.SerializeToElement(stateData, stateData.GetType()),
    };

    /// <summary>A property matched case-insensitively: behind REST the casing may differ.</summary>
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
}
