using System.Text.Json.Serialization;

namespace fiskaltrust.Middleware.SCU.GR.MyData;

public class EntityTypeOverride
{
    [JsonPropertyName("type")]
    public int? Type { get; set; }

    [JsonPropertyName("entityData")]
    public PartyTypeOverride? EntityData { get; set; }
}