using System.Text.Json.Serialization;

#pragma warning disable
namespace fiskaltrust.Middleware.Localization.QueueGR.SCU.GR.MyData.Models;

public class MiddlewareQueueGRState
{
    [JsonPropertyName("GovernmentApi")]
    [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
    public GovernmentApiData GovernmentApi { get; set; }
}
