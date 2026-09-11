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
}
