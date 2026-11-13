using System;
using System.Collections.Generic;
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

    /// <summary>
    /// The periods a closing (0x2010–0x2013) or the queue stop (0x4002) closes, each with where its totals started
    /// (<c>ftStateData.FR.Closing.Periods</c>, market-fr#221); empty when the queue handed none over.
    /// </summary>
    /// <exception cref="FormatException">A period does not read.</exception>
    public static IReadOnlyList<FRClosedPeriod> ClosedPeriods(ReceiptResponse response)
    {
        var stateData = AsJson(response.ftStateData);
        if (stateData is not { ValueKind: JsonValueKind.Object } json
            || !TryGet(json, Block, out var fr) || fr.ValueKind != JsonValueKind.Object
            || !TryGet(fr, ClosingProperty, out var closing) || closing.ValueKind != JsonValueKind.Object
            || !TryGet(closing, PeriodsProperty, out var periods) || periods.ValueKind != JsonValueKind.Array)
        {
            return Array.Empty<FRClosedPeriod>();
        }

        var result = new List<FRClosedPeriod>();
        foreach (var item in periods.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.Object
                || !TryGet(item, "Period", out var period) || period.ValueKind != JsonValueKind.String
                || !Enum.TryParse<FRTotalsPeriod>(period.GetString(), ignoreCase: true, out var parsed)
                || !TryGet(item, "Start", out var start) || start.ValueKind != JsonValueKind.String || !start.TryGetDateTime(out var moment))
            {
                throw new FormatException($"The closed period {item.GetRawText()} handed over by the queue does not read.");
            }
            result.Add(new FRClosedPeriod { Period = parsed, Start = moment });
        }
        return result;
    }

    public const string ClosingProperty = "Closing";
    public const string PeriodsProperty = "Periods";

    public const string CopyTypeProperty = "CopyType";
    public const string CopiedReceiptIdentificationProperty = "CopiedReceiptIdentification";
    public const string OriginReceiptIdentificationProperty = "OriginReceiptIdentification";
    public const string CopiedReceiptCaseProperty = "CopiedReceiptCase";

    /// <summary>On 0x4001 (Queue-start receipt): the queue's processing version, as stored with its queue item (ADR 0021).</summary>
    public const string VersionProperty = "Version";

    /// <summary>On 0x4021 (Initiate migration): the last v1.2 queue item the queue's storage holds, if any (ADR 0021).</summary>
    public const string LastV1QueueItemIdProperty = "LastV1QueueItemId";

    /// <summary>On 0x4021 (Initiate migration): the queue item of the last v1.2 archive receipt, if any (ADR 0021).</summary>
    public const string V1ArchiveQueueItemIdProperty = "V1ArchiveQueueItemId";

    /// <summary>
    /// A number of the FR block (<c>ftStateData.FR.&lt;name&gt;</c>), written as a JSON number or as a hex string
    /// (<c>0x…</c>), e.g. <see cref="CopiedReceiptCaseProperty"/>. Null when absent or unreadable.
    /// </summary>
    public static long? Number(ReceiptResponse response, string name)
    {
        var stateData = AsJson(response.ftStateData);
        if (stateData is not { ValueKind: JsonValueKind.Object } json
            || !TryGet(json, Block, out var fr) || fr.ValueKind != JsonValueKind.Object
            || !TryGet(fr, name, out var value))
        {
            return null;
        }
        if (value.ValueKind == JsonValueKind.Number && value.TryGetInt64(out var number))
        {
            return number;
        }
        if (value.ValueKind == JsonValueKind.String && value.GetString() is { } text)
        {
            var hex = text.StartsWith("0x", StringComparison.OrdinalIgnoreCase) ? text.Substring(2).Replace("_", "") : null;
            if (hex is not null && long.TryParse(hex, System.Globalization.NumberStyles.HexNumber, System.Globalization.CultureInfo.InvariantCulture, out var parsed))
            {
                return parsed;
            }
            if (long.TryParse(text, System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out parsed))
            {
                return parsed;
            }
        }
        return null;
    }

    /// <summary>
    /// A text value of the FR block (<c>ftStateData.FR.&lt;name&gt;</c>), e.g. <see cref="CopyTypeProperty"/>; an array of
    /// texts is joined with <c>,</c>. Null when absent or empty.
    /// </summary>
    public static string? Text(ReceiptResponse response, string name)
    {
        var stateData = AsJson(response.ftStateData);
        if (stateData is not { ValueKind: JsonValueKind.Object } json
            || !TryGet(json, Block, out var fr) || fr.ValueKind != JsonValueKind.Object
            || !TryGet(fr, name, out var value))
        {
            return null;
        }
        var text = value.ValueKind switch
        {
            JsonValueKind.String => value.GetString(),
            JsonValueKind.Array => string.Join(",", value.EnumerateArray().Where(x => x.ValueKind == JsonValueKind.String).Select(x => x.GetString())),
            _ => null,
        };
        return string.IsNullOrEmpty(text) ? null : text;
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
