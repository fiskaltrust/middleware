using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace fiskaltrust.Middleware.SCU.BE.ZwarteDoos.Models.Report;


public class TurnoverInput
{
    [JsonPropertyName("transactions")]
    public required List<EventTotalInput> Transactions { get; set; }

    [JsonPropertyName("departments")]
    public required List<DepartmentTotalInput> Departments { get; set; }

    [JsonPropertyName("vats")]
    public required List<VatTotalInput> Vats { get; set; }

    [JsonPropertyName("payments")]
    public required List<PaymentTotalInput> Payments { get; set; }

    [JsonPropertyName("drawersOpenCount")]
    public int DrawersOpenCount { get; set; } = 0;

    [JsonPropertyName("negQuantities")]
    public required List<NegQuantityTotalInput> NegQuantities { get; set; }

    [JsonPropertyName("priceChanges")]
    public required List<PriceChangeTotalInput> PriceChanges { get; set; }

    [JsonPropertyName("invoices")]
    public required List<InvoiceTotalInput> Invoices { get; set; }
}
