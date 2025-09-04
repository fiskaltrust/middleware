
using System.Text.Json;
using System.Text.Json.Serialization;
using fiskaltrust.ifPOS.v2;
using fiskaltrust.Middleware.Localization.v2.Models;

namespace fiskaltrust.Middleware.Localization.QueueES.Models;

public class MiddlewareState
{
    [JsonPropertyName("ES")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public MiddlewareQueueESState? ES { get; set; }

    [JsonExtensionData]
    public Dictionary<string, JsonElement> ExtraData { get; set; } = new Dictionary<string, JsonElement>();
}

public class MiddlewareQueueESState
{
    [JsonPropertyName("LastReceipt")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Receipt? LastReceipt { get; set; } = null;

    [JsonPropertyName("GovernmentAPI")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public GovernmentAPI? GovernmentAPI { get; set; } = null;
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
    public required string Version { get; set; }
}