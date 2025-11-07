using System.ComponentModel.DataAnnotations;
using System.Xml.Serialization;

namespace fiskaltrust.Middleware.Localization.QueuePT.Logic.Exports.SAFTPT.SAFTSchemaPT10401.Header;

[XmlRoot(ElementName = "CompanyAddress")]
public class CompanyAddress
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
    /// Shall include street name, building number and floor, if applicable.
    /// 
    /// Max-length 210
    /// Required
    /// </summary>
    [XmlElement(ElementName = "AddressDetail")]
    [MaxLength(210)]
    [Required]
    public required string AddressDetail { get; set; }

    /// <summary>
    /// TODO: Add documentation
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
    /// Max-length 8
    /// Required
    /// </summary>
    [XmlElement(ElementName = "PostalCode")]
    [MaxLength(8)]
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
    /// Fill in with "PT".
    /// 
    /// Max-length 2
    /// Required
    /// </summary>
    [XmlElement(ElementName = "Country")]
    [MaxLength(2)]
    [Required]
    public required string Country { get; set; }
}

