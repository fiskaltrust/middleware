using System.Text.Json.Serialization;

namespace fiskaltrust.Middleware.SCU.BE.ZwarteDoos.Models.Report;

/// <summary>
/// Represents an invoice issued during the report period.
/// </summary>
public class InvoiceTotalInput
{
    [JsonPropertyName("invoiceNo")]
    public required string InvoiceNo { get; set; }

    [JsonPropertyName("amount")]
    public required float Amount { get; set; }
}
