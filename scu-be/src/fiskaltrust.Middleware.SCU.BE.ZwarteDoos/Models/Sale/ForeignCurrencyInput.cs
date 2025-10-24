using System.Text.Json.Serialization;

namespace fiskaltrust.Middleware.SCU.BE.ZwarteDoos.Models.Sale;

public class ForeignCurrencyInput
{
    [JsonPropertyName("amount")]
    public required decimal Amount { get; set; }
    [JsonPropertyName("Iso")]
    public required string Iso { get; set; }
}
