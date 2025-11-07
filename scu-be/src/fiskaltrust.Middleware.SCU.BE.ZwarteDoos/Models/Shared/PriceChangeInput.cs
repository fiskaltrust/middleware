using System.Text.Json.Serialization;
using fiskaltrust.Middleware.SCU.BE.ZwarteDoos.Models.Enums;

namespace fiskaltrust.Middleware.SCU.BE.ZwarteDoos.Models.Shared;

public class PriceChangeInput
{
    [JsonPropertyName("groupingId")]
    public required int GroupingId { get; set; }

    [JsonPropertyName("id")]
    public required string Id { get; set; }

    [JsonPropertyName("name")]
    public required string Name { get; set; }

    [JsonPropertyName("scope")]
    public required PriceChangeScope Scope { get; set; }

    [JsonPropertyName("type")]
    public required PriceChangeType Type { get; set; }

    [JsonPropertyName("amount")]
    public required decimal Amount { get; set; }
}
