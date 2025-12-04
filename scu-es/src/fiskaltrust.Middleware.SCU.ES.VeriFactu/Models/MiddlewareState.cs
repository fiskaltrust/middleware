using System.Text.Json;
using System.Text.Json.Serialization;
using fiskaltrust.ifPOS.v2;

namespace fiskaltrust.Middleware.SCU.ES.VeriFactu.Models;

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

    public static MiddlewareStateData FromReceiptResponse(ReceiptResponse receiptResponse) => JsonSerializer.Deserialize<MiddlewareStateData>(((JsonElement) receiptResponse.ftStateData!).GetRawText())!;
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

public class MiddlewareStateDataES
{
    [JsonPropertyName("LastReceipt")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Receipt? LastReceipt { get; set; } = null;


    [JsonPropertyName("SerieFactura")]
    public required string SerieFactura { get; set; }

    [JsonPropertyName("NumFactura")]
    public required ulong NumFactura { get; set; }

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
    public required string Request { get; set; }

    [JsonPropertyName("Response")]
    [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
    public required string Response { get; set; }

    [JsonPropertyName("Version")]
    [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public required GovernmentAPISchemaVersion Version { get; set; }
}