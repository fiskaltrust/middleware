using System.Text.Json.Serialization;
using fiskaltrust.Middleware.SCU.BE.ZwarteDoos.Models.Enums;

namespace fiskaltrust.Middleware.SCU.BE.ZwarteDoos.Models.Shared;

public class CostCenterInput
{
    [JsonPropertyName("id")]
    public required string Id { get; set; }

    [JsonPropertyName("type")]
    public required CostCenterType Type { get; set; }

    [JsonPropertyName("reference")]
    public required string Reference { get; set; }

    [JsonPropertyName("costCenter")]
    public CostCenterInput? CostCenter { get; set; }
}
