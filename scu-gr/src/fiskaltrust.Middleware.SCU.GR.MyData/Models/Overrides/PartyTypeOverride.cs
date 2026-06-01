using System.Text.Json.Serialization;

namespace fiskaltrust.Middleware.SCU.GR.MyData;

public class PartyTypeOverride
{
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
