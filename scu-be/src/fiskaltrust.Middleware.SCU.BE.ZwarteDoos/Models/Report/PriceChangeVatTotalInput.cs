using System.Text.Json.Serialization;
using fiskaltrust.Middleware.SCU.BE.ZwarteDoos.Models.Enums;

namespace fiskaltrust.Middleware.SCU.BE.ZwarteDoos.Models.Report;

/// <summary>
/// Contains the totals for a VAT rate of a price change that was applied during the report period.
/// </summary>
public class PriceChangeVatTotalInput
{
    [JsonPropertyName("label")]
    public required VatLabel Label { get; set; }

    [JsonPropertyName("negative")]
    public required decimal Negative { get; set; }

    [JsonPropertyName("positive")]
    public required decimal Positive { get; set; }
}
