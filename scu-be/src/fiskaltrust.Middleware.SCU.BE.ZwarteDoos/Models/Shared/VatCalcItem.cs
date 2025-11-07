using System.Text.Json.Serialization;
using fiskaltrust.Middleware.SCU.BE.ZwarteDoos.Models.Enums;

namespace fiskaltrust.Middleware.SCU.BE.ZwarteDoos.Models.Shared;

public class VatCalcItem
{
    [JsonPropertyName("label")]
    public required VatLabel Label { get; set; }

    [JsonPropertyName("rate")]
    public required decimal Rate { get; set; }

    [JsonPropertyName("taxableAmount")]
    public required decimal TaxableAmount { get; set; }

    [JsonPropertyName("vatAmount")]
    public required decimal VatAmount { get; set; }

    [JsonPropertyName("totalAmount")]
    public required decimal TotalAmount { get; set; }

    [JsonPropertyName("outOfScope")]
    public required bool OutOfScope { get; set; }
}
