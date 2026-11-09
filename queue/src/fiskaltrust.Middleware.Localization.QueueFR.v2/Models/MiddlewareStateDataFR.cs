using System.Text.Json;
using System.Text.Json.Serialization;
using fiskaltrust.ifPOS.v2;
using SharedStateData = fiskaltrust.Middleware.Localization.v2.Models.MiddlewareStateData;

namespace fiskaltrust.Middleware.Localization.QueueFR.v2.Models;

/// <summary>
/// The <c>ftStateData</c> of a French receipt response: the shared middleware part (previous receipt
/// references) plus an <c>FR</c> block for what only France reports. Mirrors the pattern of the ES queue.
/// </summary>
public class MiddlewareStateData : SharedStateData
{
    public MiddlewareStateData() { }

    private MiddlewareStateData(SharedStateData middlewareStateData) : base(middlewareStateData)
    {
        // The shared type knows nothing of the FR block, so a block that came in as JSON sits in its
        // extension data. Lift it into the typed property so that it is never written twice.
        if (ExtraData.Remove("FR", out var fr))
        {
            FR = fr.Deserialize<MiddlewareStateDataFR>();
        }
    }

    [JsonPropertyName("FR")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public MiddlewareStateDataFR? FR { get; set; }

    /// <summary>
    /// Reads the state data already on the response - the shared <c>SignProcessor</c> may have put the
    /// previous receipt references there - so that adding the FR block never drops it.
    /// </summary>
    public new static MiddlewareStateData FromReceiptResponse(ReceiptResponse receiptResponse) => receiptResponse.ftStateData switch
    {
        MiddlewareStateData fr => fr,
        JsonElement json => JsonSerializer.Deserialize<MiddlewareStateData>(json.GetRawText()) ?? new MiddlewareStateData(),
        SharedStateData shared => new MiddlewareStateData(shared),
        _ => new MiddlewareStateData(),
    };
}

public class MiddlewareStateDataFR
{
    /// <summary>
    /// What the receipt sold: <c>B</c> goods (biens), <c>S</c> services, <c>M</c> both. Omitted when the
    /// receipt has neither, see <see cref="Logic.FRTypeOfServiceCalculator"/>.
    /// </summary>
    [JsonPropertyName("ftTypeOfService")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ftTypeOfService { get; set; }

    /// <summary>
    /// The reprint number of a copy (0x3010): 1 for the first fiscal copy of the copied receipt
    /// (R13 ENC-DUP-PRN-NUM, market-fr#235). Every FR SCU reads it from here and signs it.
    /// Omitted on every other receipt and on a training copy.
    /// </summary>
    [JsonPropertyName(fiskaltrust.Middleware.SCU.FR.Abstraction.Helpers.FRStateData.CopyNumberProperty)]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? CopyNumber { get; set; }

    /// <summary>
    /// The boundaries of the archive an archive receipt (0x3011) closes, as v1.2 signed them
    /// (market-fr#221). Every FR SCU reads them from here and signs them. Omitted on every other
    /// receipt and on a training archive receipt.
    /// </summary>
    [JsonPropertyName(fiskaltrust.Middleware.SCU.FR.Abstraction.Helpers.FRStateData.ArchiveProperty)]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public fiskaltrust.ifPOS.v2.fr.FRArchiveBoundaries? Archive { get; set; }
}
