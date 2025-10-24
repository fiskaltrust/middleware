using System.Text.Json.Serialization;

namespace fiskaltrust.Middleware.SCU.BE.ZwarteDoos.Models.Enums;

// Enums

/// <summary>
/// The language the POS system is operating in.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum Language
{
    [JsonPropertyName("NL")]
    NL,
    [JsonPropertyName("FR")]
    FR,
    [JsonPropertyName("DE")]
    DE
}
