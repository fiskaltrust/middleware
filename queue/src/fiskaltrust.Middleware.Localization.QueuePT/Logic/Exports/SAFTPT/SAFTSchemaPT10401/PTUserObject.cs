using System.Text.Json.Serialization;

namespace fiskaltrust.Middleware.Localization.QueuePT.Logic.Exports.SAFTPT.SAFTSchemaPT10401;

public class PTUserObject
{
    [JsonPropertyName("UserId")]
    public string? UserId { get; set; }
    [JsonPropertyName("UserDisplayName")]
    public string? UserDisplayName { get; set; }
}
