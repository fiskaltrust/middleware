using System.Collections.Generic;
using System.Text.Json.Serialization;
using fiskaltrust.Middleware.SCU.BE.ZwarteDoos.Models.Enums;

namespace fiskaltrust.Middleware.SCU.BE.ZwarteDoos.Models.Report;

/// <summary>
/// Contains amounts and counters for a payment method and PaymentLineType combination.
/// </summary>
public class PaymentTotalAmountInput
{
    [JsonPropertyName("type")]
    public required PaymentLineType Type { get; set; }

    [JsonPropertyName("normalAmount")]
    public required decimal NormalAmount { get; set; }

    [JsonPropertyName("negativeCorrections")]
    public required decimal NegativeCorrections { get; set; }

    [JsonPropertyName("positiveCorrections")]
    public required decimal PositiveCorrections { get; set; }

    [JsonPropertyName("correctionsCount")]
    public required int CorrectionsCount { get; set; }

    [JsonPropertyName("totalAmount")]
    public required decimal TotalAmount { get; set; }

    [JsonPropertyName("normalForeign")]
    public List<ForeignCurrencyInput>? NormalForeign { get; set; }

    [JsonPropertyName("correctionsForeign")]
    public List<ForeignCurrencyInput>? CorrectionsForeign { get; set; }
}
