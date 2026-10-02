using System.Text.Json.Serialization;

namespace fiskaltrust.Middleware.SCU.GR.MyData;

public class PackagingDetailTypeOverride
{
    [JsonPropertyName("packagingType")]
    public int? PackagingType { get; set; }

    [JsonPropertyName("quantity")]
    public int? Quantity { get; set; }

    [JsonPropertyName("otherPackagingTypeTitle")]
    public string? OtherPackagingTypeTitle { get; set; }
}