using System.Text.Json;
using System.Text.Json.Serialization;

namespace fiskaltrust.Middleware.Localization.QueueIT;

public class QueueITConfiguration
{
    public bool Sandbox { get; set; }

    [JsonPropertyName("scu-timeout-ms")]
    public long? ScuTimeoutMs { get; set; }

    [JsonPropertyName("scu-max-retries")]
    public int? ScuMaxRetries { get; set; } = 1;

    public static QueueITConfiguration FromConfiguration(Dictionary<string, object> configuration)
        => JsonSerializer.Deserialize<QueueITConfiguration>(JsonSerializer.Serialize(configuration)) ?? new QueueITConfiguration();
}
