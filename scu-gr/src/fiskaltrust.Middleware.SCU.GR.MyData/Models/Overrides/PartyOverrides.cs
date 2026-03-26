using System.Text.Json.Serialization;

namespace fiskaltrust.Middleware.SCU.GR.MyData;

public class PartyUnmappedFieldsOverride
{
    [JsonPropertyName("documentIdNo")]
    public string? DocumentIdNo { get; set; }

    [JsonPropertyName("supplyAccountNo")]
    public string? SupplyAccountNo { get; set; }

    [JsonPropertyName("countryDocumentId")]
    public string? CountryDocumentId { get; set; }
}

public class EntityOverride
{
    [JsonPropertyName("type")]
    public int? Type { get; set; }

    [JsonPropertyName("entityData")]
    public PartyOverride? EntityData { get; set; }
}

public class PartyOverride
{
    [JsonPropertyName("vatNumber")]
    public string? VatNumber { get; set; }

    [JsonPropertyName("country")]
    public string? Country { get; set; }

    [JsonPropertyName("branch")]
    public int? Branch { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("address")]
    public AddressOverride? Address { get; set; }

    [JsonPropertyName("documentIdNo")]
    public string? DocumentIdNo { get; set; }

    [JsonPropertyName("supplyAccountNo")]
    public string? SupplyAccountNo { get; set; }

    [JsonPropertyName("countryDocumentId")]
    public string? CountryDocumentId { get; set; }
}
