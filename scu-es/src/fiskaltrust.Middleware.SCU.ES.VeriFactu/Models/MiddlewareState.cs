using System.Text.Json;
using System.Text.Json.Serialization;
using fiskaltrust.ifPOS.v2;

namespace fiskaltrust.Middleware.SCU.ES.Models;

public class MiddlewareState
{
    [JsonPropertyName("ES")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public MiddlewareQueueESState? ES { get; set; }

    [JsonPropertyName("ftPreviousReceiptReference")]
    [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
    public List<Receipt> PreviousReceiptReference { get; set; } = new();

    [JsonExtensionData]
    public Dictionary<string, JsonElement> ExtraData { get; set; } = new Dictionary<string, JsonElement>();
}

public class Receipt
{
    [JsonPropertyName("Response")]
    [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
    public ReceiptResponse Response { get; set; } = null!;

    [JsonPropertyName("Request")]
    [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
    public ReceiptRequest Request { get; set; } = null!;
}

public class MiddlewareQueueESState
{
    [JsonPropertyName("LastReceipt")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public LastReceipt? LastReceipt { get; set; } = null;

    [JsonPropertyName("GovernmentAPI")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public GovernmentAPI? GovernmentAPI { get; set; } = null;
}

public class LastReceipt
{
    [JsonPropertyName("Request")]
    [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
    public required ReceiptRequest Request { get; set; }

    [JsonPropertyName("Response")]
    [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
    public required ReceiptResponse Response { get; set; }
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