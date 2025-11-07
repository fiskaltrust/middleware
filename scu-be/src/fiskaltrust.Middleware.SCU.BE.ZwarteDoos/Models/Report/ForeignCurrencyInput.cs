using System.Text.Json.Serialization;

namespace fiskaltrust.Middleware.SCU.BE.ZwarteDoos.Models.Report;

/// <summary>
/// Represents an amount in a foreign currency.
/// </summary>
public class ForeignCurrencyInput
{
    [JsonPropertyName("amount")]
    public required float Amount { get; set; }

    [JsonPropertyName("iso")]
    public required string Iso { get; set; }
}
