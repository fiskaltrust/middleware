using System.Text.Json;
using System.Text.Json.Serialization;
using fiskaltrust.ifPOS.v1.me;
using fiskaltrust.ifPOS.v2;

namespace fiskaltrust.Middleware.Localization.v2.Models;

public class MiddlewareStateDataBase<T> where T : MiddlewareStateDataBase<T>
{
    [JsonExtensionData]
    public Dictionary<string, JsonElement> ExtraData { get; set; } = new Dictionary<string, JsonElement>();

    public static T FromReceiptResponse(ReceiptResponse receiptResponse) => JsonSerializer.Deserialize<T>(((JsonElement) receiptResponse.ftStateData!).GetRawText())!;
}

public class MiddlewareStateData : MiddlewareStateDataBase<MiddlewareStateData>
{
    [JsonPropertyName("ftPreviousReceiptReference")] // QUESTION: ftPreviousReceiptReferences or ftPreviousReceiptReference?
    [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
    public List<Receipt>? PreviousReceiptReference { get; set; }

    [JsonExtensionData]
    public Dictionary<string, JsonElement> ExtraData { get; set; } = new Dictionary<string, JsonElement>();
}

public class Receipt
{
    [JsonPropertyName("Response")]
    [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
    public required ReceiptResponse Response { get; set; }

    [JsonPropertyName("Request")]
    [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
    public required ReceiptRequest Request { get; set; }
}