using System.ComponentModel.DataAnnotations;
using System.Xml.Serialization;

namespace fiskaltrust.Middleware.Localization.QueuePT.Logic.Exports.SAFTPT.SAFTSchemaPT10401.MasterFiles;

[XmlRoot(ElementName = "BillingAddress")]
public class BillingAddress
{

    /// <summary>
    /// TODO: Add documentation
    /// 
    /// Max-length 10
    /// </summary>
    [XmlElement(ElementName = "BuildingNumber")]
    [MaxLength(10)]
    public string? BuildingNumber { get; set; }

    /// <summary>
    /// TODO: Add documentation
    /// 
    /// Max-length 200
    /// </summary>
    [XmlElement(ElementName = "StreetName")]
    [MaxLength(200)]
    public string? StreetName { get; set; }

    /// <summary>
    /// The field shall include the street name, the building number and floor, if applicable.
    /// 
    /// The field shall be filled in with the designation "Desconhecido" (Unknown) in the following cases:
    /// - Non-integrated systems, if information is not known;
    /// - Operations carried out with the "Consumidor final" (Final Consumer).
    /// 
    /// Max-length 210
    /// Required
    /// </summary>
    [MaxLength(210)]
    [XmlElement(ElementName = "AddressDetail")]
    public required string AddressDetail { get; set; }

    /// <summary>
    /// TODO: Add documentation
    /// 
    /// The field shall be filled in with the designation "Desconhecido" (Unknown) in the following cases:
    /// - Non-integrated systems, if information is not known;
    /// - Operations carried out with the "Consumidor final" (Final Consumer).
    /// 
    /// Max-length 50
    /// Required
    /// </summary>
    [XmlElement(ElementName = "City")]
    [MaxLength(50)]
    [Required]
    public required string City { get; set; }

    /// <summary>
    /// TODO: Add documentation
    /// 
    /// The field shall be filled in with the designation "Desconhecido" (Unknown) in the following cases:
    /// - Non-integrated systems, if information is not known;
    /// - Operations carried out with the "Consumidor final" (Final Consumer).
    /// 
    /// Max-length 20
    /// Required
    /// </summary>
    [XmlElement(ElementName = "PostalCode")]
    [MaxLength(20)]
    [Required]
    public required string PostalCode { get; set; }

    /// <summary>
    /// TODO: Add documentation
    /// 
    /// Max-length 50
    /// </summary>
    [XmlElement(ElementName = "Region")]
    [MaxLength(50)]
    public string? Region { get; set; }

    /// <summary>
    /// If it is known, the field shall be filled in according to norm ISO 3166-1-alpha-2.
    /// 
    /// The field shall include the street name, the building number and floor, if applicable.
    /// 
    /// The field shall be filled in with the designation "Desconhecido" (Unknown) in the following cases:
    /// - Non-integrated systems, if information is not known;
    /// - Operations carried out with the "Consumidor final" (Final Consumer).
    /// 
    /// Max-length 12
    /// Required
    /// </summary>
    [XmlElement(ElementName = "Country")]
    [MaxLength(12)]
    [Required]
    public required string Country { get; set; }
}