using System.Text.Json.Serialization;

namespace fiskaltrust.Middleware.Localization.v2.Configuration;

public class ftCashBoxConfiguration
{
    [JsonPropertyName("helpers")]
    public List<PackageConfiguration>? helpers { get; set; }

    [JsonPropertyName("ftCashBoxId")]
    public Guid ftCashBoxId { get; private set; }

    [JsonPropertyName("ftSignaturCreationDevices")]
    public List<PackageConfiguration>? ftSignaturCreationDevices { get; set; }

    [JsonPropertyName("ftQueues")]
    public List<PackageConfiguration>? ftQueues { get; set; }

    [JsonPropertyName("PackTimeStampage")]
    public long TimeStamp { get; set; }
}
