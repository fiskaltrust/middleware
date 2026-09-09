using System.Text.Json.Serialization;

namespace fiskaltrust.Middleware.SCU.GR.MyData;

public class PartyTypeOverride
{
    // used for otherCorrelatedEntities entry (a correlated party has no cbCustomer to fill them in).
    [JsonPropertyName("vatNumber")]
    public string? VatNumber { get; set; }

    [JsonPropertyName("country")]
    public string? Country { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("branch")]
    public int? Branch { get; set; }

    [JsonPropertyName("address")]
    public AddressTypeOverride? Address { get; set; }

    [JsonPropertyName("documentIdNo")]
    public string? DocumentIdNo { get; set; }

    [JsonPropertyName("supplyAccountNo")]
    public string? SupplyAccountNo { get; set; }

    [JsonPropertyName("countryDocumentId")]
    public string? CountryDocumentId { get; set; }
}
