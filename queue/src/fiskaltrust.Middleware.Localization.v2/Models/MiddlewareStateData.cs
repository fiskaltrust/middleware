using System.Text.Json;
using System.Text.Json.Serialization;
using fiskaltrust.ifPOS.v1.me;
using fiskaltrust.ifPOS.v2;

namespace fiskaltrust.Middleware.Localization.v2.Models;

public class MiddlewareStateDataBase<T> where T : MiddlewareStateDataBase<T>
{
    [JsonExtensionData]
    public Dictionary<string, JsonElement> ExtraData { get; set; } = new Dictionary<string, JsonElement>();

    public static T? FromReceiptResponse(ReceiptResponse receiptResponse)
    {
        if (receiptResponse.ftStateData is null)
        {
            return null;
        }

        if (receiptResponse.ftStateData is JsonElement jsonElement)
        {
            return JsonSerializer.Deserialize<T>(jsonElement.GetRawText());
        }

        else if (receiptResponse.ftStateData is T data)
        {
            return data;
        }

        return null;
    }
}

public class MiddlewareStateData : MiddlewareStateDataBase<MiddlewareStateData>
{
    public MiddlewareStateData()
    {
    }

    public MiddlewareStateData(MiddlewareStateData middlewareStateData)
    {
        ExtraData = middlewareStateData.ExtraData;
        PreviousReceiptReference = middlewareStateData.PreviousReceiptReference;
    }

    [JsonPropertyName("ftPreviousReceiptReference")] // QUESTION: ftPreviousReceiptReferences or ftPreviousReceiptReference?
    [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
    public List<Receipt>? PreviousReceiptReference { get; set; }

    [JsonExtensionData]
    public Dictionary<string, JsonElement> ExtraData { get; set; } = new Dictionary<string, JsonElement>();
}

public class Receipt
{
    [JsonPropertyName("ReceiptRequest")]
    [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
    public required ReceiptRequest Request { get; set; }

    [JsonPropertyName("ReceiptResponse")]
    [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
    public required ReceiptResponse Response { get; set; }
}