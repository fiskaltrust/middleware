using System.Text.Json.Serialization;

namespace fiskaltrust.Middleware.Localization.QueueGR.SCU.GR.MyData.Models;

public class MiddlewareSCUGRMyDataState
{
    [JsonPropertyName("GR")]
    [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
    public MiddlewareQueueGRState GR { get; set; } = new MiddlewareQueueGRState();

    [JsonExtensionData]
    public Dictionary<string, object> AdditionalData { get; set; } = new Dictionary<string, object>();
}
