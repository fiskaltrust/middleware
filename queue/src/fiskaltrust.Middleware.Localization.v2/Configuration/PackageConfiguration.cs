using System.Text.Json.Serialization;

namespace fiskaltrust.Middleware.Localization.v2.Configuration;

public class PackageConfiguration
{
    [JsonPropertyName("Id")]
    public Guid Id { get; set; }

    [JsonPropertyName("Package")]
    public string Package { get; set; }

    [JsonPropertyName("Version")]
    public string Version { get; set; }

    [JsonPropertyName("Configuration")]
    public Dictionary<string, object>? Configuration { get; set; }

    [JsonPropertyName("Url")]
    public List<string>? Url { get; set; }

    public PackageConfiguration()
    {
        Id = Guid.Empty;
        Package = string.Empty;
        Version = string.Empty;
        Configuration = null;
        Url = null;
    }
}