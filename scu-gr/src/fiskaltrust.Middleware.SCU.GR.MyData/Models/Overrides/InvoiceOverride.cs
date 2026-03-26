using System.Text.Json.Serialization;

namespace fiskaltrust.Middleware.SCU.GR.MyData;

public class InvoiceOverride
{
    [JsonPropertyName("invoiceHeader")]
    public InvoiceHeaderOverride? InvoiceHeader { get; set; }

    [JsonPropertyName("counterpart")]
    public PartyUnmappedFieldsOverride? Counterpart { get; set; }

    [JsonPropertyName("issuer")]
    public PartyUnmappedFieldsOverride? Issuer { get; set; }
}
