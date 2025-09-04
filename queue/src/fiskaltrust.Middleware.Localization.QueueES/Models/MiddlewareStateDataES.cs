
using System.Text.Json;
using System.Text.Json.Serialization;
using fiskaltrust.ifPOS.v2;

namespace fiskaltrust.Middleware.Localization.QueueES.Models;

public class MiddlewareStateData : v2.Models.MiddlewareStateDataBase<MiddlewareStateData>
{
    [JsonPropertyName("ES")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public MiddlewareStateDataES? ES { get; set; }
}

public class MiddlewareStateDataES
{
    [JsonPropertyName("LastReceipt")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public v2.Models.Receipt? LastReceipt { get; set; } = null;

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