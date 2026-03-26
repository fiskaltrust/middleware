using System.Text.Json.Serialization;

namespace fiskaltrust.Middleware.SCU.GR.MyData;

public class ReceiptRequestMyDataOverride
{
    [JsonPropertyName("invoice")]
    public InvoiceOverride? Invoice { get; set; }
}
