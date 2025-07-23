using System.Text.Json.Serialization;

namespace fiskaltrust.Middleware.Localization.QueueGR.SCU.GR.MyData.Models;

public class MiddlewareState
{
    [JsonPropertyName("GR")]
    [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
    public MiddlewareQueueGRState GR { get; set; } = new MiddlewareQueueGRState();  
}
