using System;
using System.Text.Json.Serialization;

namespace fiskaltrust.Middleware.SCU.GR.MyData;

public class ShipOverride
{
    [JsonPropertyName("applicationId")]
    public string? ApplicationId { get; set; }

    [JsonPropertyName("applicationDate")]
    public DateTime? ApplicationDate { get; set; }

    [JsonPropertyName("doy")]
    public string? Doy { get; set; }

    [JsonPropertyName("shipId")]
    public string? ShipId { get; set; }
}
