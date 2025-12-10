using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using fiskaltrust.ifPOS.v2;

namespace fiskaltrust.Middleware.SCU.ES.Common.Models;

public class MiddlewareStateData
{
    [JsonPropertyName("ES")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public MiddlewareStateDataES? ES { get; set; }

    [JsonPropertyName("ftPreviousReceiptReference")]
    [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
    public List<Receipt>? PreviousReceiptReference { get; set; } = new();

    [JsonExtensionData]
    public Dictionary<string, JsonElement> ExtraData { get; set; } = new Dictionary<string, JsonElement>();

    public static MiddlewareStateData FromReceiptResponse(ReceiptResponse receiptResponse) => JsonSerializer.Deserialize<MiddlewareStateData>(((JsonElement)receiptResponse.ftStateData!).GetRawText())!;
}

public class Receipt
{
    [JsonPropertyName("ReceiptRequest")]
    [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
    public ReceiptRequest Request { get; set; } = null!;

    [JsonPropertyName("ReceiptResponse")]
    [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
    public ReceiptResponse Response { get; set; } = null!;
}

public class MiddlewareStateDataES
{
    [JsonPropertyName("LastReceipt")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Receipt? LastReceipt { get; set; } = null;

    [JsonPropertyName("GovernmentAPI")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public GovernmentAPI? GovernmentAPI { get; set; } = null;
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum GovernmentAPISchemaVersion
{
    V0
}

public class GovernmentAPI
{
    [JsonPropertyName("Request")]
    [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
    public string Request { get; set; } = null!;

    [JsonPropertyName("Response")]
    [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
    public string Response { get; set; } = null!;

    [JsonPropertyName("Version")]
    [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public GovernmentAPISchemaVersion Version { get; set; }
}