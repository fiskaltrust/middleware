using System.Collections.Generic;
using System.Text.Json.Serialization;
using fiskaltrust.Middleware.SCU.BE.ZwarteDoos.Models.Enums;

namespace fiskaltrust.Middleware.SCU.BE.ZwarteDoos.Models.Report;

/// <summary>
/// Contains a price change that was applied during the report period. Only price changes of type PUBLIC should be included.
/// </summary>
public class PriceChangeTotalInput
{
    [JsonPropertyName("id")]
    public required string Id { get; set; }

    [JsonPropertyName("name")]
    public required string Name { get; set; }

    [JsonPropertyName("type")]
    public required PriceChangeType Type { get; set; }

    [JsonPropertyName("amount")]
    public required List<PriceChangeVatTotalInput> Amount { get; set; }
}
