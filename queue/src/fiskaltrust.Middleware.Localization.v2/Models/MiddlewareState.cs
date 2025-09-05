using System.Text.Json;
using System.Text.Json.Serialization;
using fiskaltrust.ifPOS.v2;

namespace fiskaltrust.Middleware.Localization.v2.Models;

public class MiddlewareState
{
    [JsonExtensionData]
    public Dictionary<string, JsonElement> ExtraData { get; set; } = new Dictionary<string, JsonElement>();

    [JsonPropertyName("ftPreviousReceiptReference")] // QUESTION: ftPreviousReceiptReferences or ftPreviousReceiptReference?
    [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
    public List<Receipt> PreviousReceiptReference { get; set; }
}

public class Receipt
{
    [JsonPropertyName("Response")]
    [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
    public ReceiptResponse Response { get; set; }

    [JsonPropertyName("Request")]
    [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
    public ReceiptRequest Request { get; set; }
}