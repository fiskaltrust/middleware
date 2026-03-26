using System.Text.Json.Serialization;

namespace fiskaltrust.Middleware.SCU.GR.MyData;

public class ChargeItemMyDataOverride
{
    [JsonPropertyName("invoiceDetails")]
    public InvoiceDetailOverride? InvoiceDetails { get; set; }
}
