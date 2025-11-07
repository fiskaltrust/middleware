using System.Text.Json.Serialization;

namespace fiskaltrust.Middleware.SCU.BE.ZwarteDoos.Models.Device;

public class DeviceInfo
{
    [JsonPropertyName("id")]
    public required string Id { get; set; }
}