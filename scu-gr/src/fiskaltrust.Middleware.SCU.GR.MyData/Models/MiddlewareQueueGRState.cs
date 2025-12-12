using System.Text.Json.Serialization;

#pragma warning disable
namespace fiskaltrust.Middleware.SCU.GR.MyData.Models;

public class MiddlewareQueueGRState
{
    [JsonPropertyName("GovernmentApi")]
    [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
    public GovernmentApiData GovernmentApi { get; set; }
}