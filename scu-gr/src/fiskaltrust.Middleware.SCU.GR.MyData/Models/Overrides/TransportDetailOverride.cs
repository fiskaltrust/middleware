using System.Text.Json.Serialization;

namespace fiskaltrust.Middleware.SCU.GR.MyData;

public class TransportDetailOverride
{
    [JsonPropertyName("vehicleNumber")]
    public string? VehicleNumber { get; set; }
}
