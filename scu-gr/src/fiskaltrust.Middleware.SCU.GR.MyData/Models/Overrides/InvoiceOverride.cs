using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace fiskaltrust.Middleware.SCU.GR.MyData;

public class InvoiceOverride
{
    [JsonPropertyName("issuer")]
    public PartyTypeOverride? Issuer { get; set; }

    [JsonPropertyName("counterpart")]
    public PartyTypeOverride? Counterpart { get; set; }

    [JsonPropertyName("invoiceHeader")]
    public InvoiceHeaderTypeOverride? InvoiceHeader { get; set; }

    [JsonPropertyName("otherTransportDetails")]
    public List<TransportDetailOverride>? OtherTransportDetails { get; set; }
}
