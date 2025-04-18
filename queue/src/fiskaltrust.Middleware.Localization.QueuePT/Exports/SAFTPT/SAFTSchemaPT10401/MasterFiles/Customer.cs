using System.ComponentModel.DataAnnotations;
using System.Xml.Serialization;

namespace fiskaltrust.SAFT.CLI.SAFTSchemaPT10401;
#pragma warning disable
[XmlRoot(ElementName = "Customer")]
public class Customer
{
    /// <summary>
    /// In the list of clients cannot exist more than one registration with the same CustomerID. In the case of final consumers, a generic client with the designation of “Consumidor final” (Final Consumer) shall be created.
    /// 
    /// Max-length 30
    /// Required
    /// </summary>
    [XmlElement(ElementName = "CustomerID")]
    [MaxLength(30)]
    [Required]
    public required string CustomerID { get; set; }

    /// <summary>
    /// The respective client’s current account must be indicated in the general accounting plan, if it is defined. Otherwise the field shall be filled in with the designation "Desconhecido" (Unknown).
    /// 
    /// Max-length 30
    /// Required
    /// </summary>
    [XmlElement(ElementName = "AccountID")]
    [MaxLength(30)]
    [Required]
    public required string AccountID { get; set; }

    /// <summary>
    /// It must be indicated without the country’s prefix.
    /// 
    /// The generic client, corresponding to the aforementioned "Consumidor final" (Final consumer) shall be identified with the Tax Identification Number "999999990".
    /// 
    /// Max-length 30
    /// Required
    /// </summary>
    [XmlElement(ElementName = "CustomerTaxID")]
    [MaxLength(30)]
    [Required]
    public required string CustomerTaxID { get; set; }

    /// <summary>
    /// The generic client shall be identified with the designation “Consumidor final” (Final Consumer).
    /// 
    /// Max-length 100
    /// Required
    /// </summary>
    [XmlElement(ElementName = "CompanyName")]
    [MaxLength(100)]
    [Required]
    public required string CompanyName { get; set; }

    /// <summary>
    /// TODO: Add documentation
    /// 
    /// Max-length 50
    /// </summary>
    [XmlElement(ElementName = "Contact")]
    [MaxLength(30)]
    public string? Contact { get; set; }

    /// <summary>
    /// Head office address or the fixed /permanent establishment address, located on Portuguese territory.
    /// 
    /// Required
    /// </summary>
    [XmlElement(ElementName = "BillingAddress")]
    [Required]
    public required BillingAddress BillingAddress { get; set; }

    /// <summary>
    /// TODO: Add documentation
    /// 
    /// Max-length: 20
    /// </summary>
    [XmlElement(ElementName = "Telephone")]
    [MaxLength(20)]
    public string? Telephone { get; set; }

    /// <summary>
    /// TODO: Add documentation
    /// 
    /// Max-length: 20
    /// </summary>
    [XmlElement(ElementName = "Fax")]
    [MaxLength(20)]
    public string? Fax { get; set; }

    /// <summary>
    /// Companies e-mail address.
    /// 
    /// Max-length: 254
    /// </summary>
    [XmlElement(ElementName = "Email")]
    [MaxLength(254)]
    public string Email { get; set; }

    /// <summary>
    /// Companies Website
    /// 
    /// Max-length: 60
    /// </summary>
    [XmlElement(ElementName = "Website")]
    [MaxLength(60)]
    public string? Website { get; set; }

    /// <summary>
    /// Indicator of the existence of a self-billing agreement between the customer and the supplier.
    /// The field shall be filled in with "1" if there is an agreement and with "0" (zero) if there is not one.
    /// 
    /// Required
    /// </summary>
    [XmlElement(ElementName = "SelfBillingIndicator")]
    [Required]
    public int SelfBillingIndicator { get; set; }
}

