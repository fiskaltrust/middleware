using System.Text.Json.Serialization;

namespace fiskaltrust.Middleware.SCU.GR.MyData;

public class PackingsDeclarationOverride
{
    [JsonPropertyName("Packages")]
    public PackagingDetailTypeOverride[]? Packages { get; set; }
}