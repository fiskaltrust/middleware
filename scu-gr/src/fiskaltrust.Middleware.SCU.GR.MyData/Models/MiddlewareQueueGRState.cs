using System.Text.Json.Serialization;

#pragma warning disable
namespace fiskaltrust.Middleware.SCU.GR.MyData.Models;

public class MiddlewareQueueGRState
{
    [JsonPropertyName("GovernmentApi")]
    [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
    public GovernmentApiData GovernmentApi { get; set; }

    [JsonPropertyName("ProposedInvoiceCounter")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ProposedInvoiceCounter? ProposedInvoiceCounter { get; set; }
}

public class ProposedInvoiceCounter
{
    [JsonPropertyName("Series")]
    public string Series { get; set; } = "";

    [JsonPropertyName("Aa")]
    public long Aa { get; set; }
}