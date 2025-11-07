using System.Text.Json.Serialization;

namespace fiskaltrust.Middleware.SCU.BE.ZwarteDoos.Models.Device;

public class DeviceResponse
{
    [JsonPropertyName("device")]
    public DeviceInfo Device { get; set; } = null!;
}
