using System.Xml.Serialization;

namespace fiskaltrust.Middleware.Localization.QueuePT.Exports.SAFTPT.SAFTSchemaPT10401.SourceDocumentContracts;

[XmlRoot(ElementName = "Address")]
public class Address
{
    [XmlElement(ElementName = "BuildingNumber")]
    public string? BuildingNumber { get; set; }

    [XmlElement(ElementName = "StreetName")]
    public string? StreetName { get; set; }

    [XmlElement(ElementName = "AddressDetail")]
    public required string AddressDetail { get; set; }

    [XmlElement(ElementName = "City")]
    public required string City { get; set; }

    [XmlElement(ElementName = "PostalCode")]
    public required string PostalCode { get; set; }

    [XmlElement(ElementName = "Region")]
    public string? Region { get; set; }

    [XmlElement(ElementName = "Country")]
    public required string Country { get; set; }
}

