using System.ComponentModel.DataAnnotations;
using System.Xml.Serialization;

namespace fiskaltrust.Middleware.Localization.QueuePT.Logic.Exports.SAFTPT.SAFTSchemaPT10401.MasterFiles;

[XmlRoot(ElementName = "Product")]
public class Product
{
    /// <summary>
    /// The field shall be filled in with:
    /// "P" - Products;
    /// "S" - Services;
    /// "O" - Others (e.g. charged freights, advance payments received or sale of assets);
    /// "E" - Excise duties - (e.g. IABA, ISP, IT);
    /// "I" - Taxes, tax rates and parafiscal charges except VAT and Stamp Duty which shall appear in table 2.5. – TaxTable and Excise Duties which shall be filled in with the "E" code.
    /// 
    /// Max-length: 1
    /// </summary>
    [XmlElement(ElementName = "ProductType")]
    [MaxLength(1)]
    [Required]
    public required string ProductType { get; set; }

    /// <summary>
    /// The unique code in the list of products.
    /// 
    /// Max-length: 60
    /// </summary>
    [XmlElement(ElementName = "ProductCode")]
    [MaxLength(60)]
    [Required]
    public required string ProductCode { get; set; }


    /// <summary>
    /// TODO: Add documentation
    /// 
    /// Max-length: 50
    /// </summary>
    [XmlElement(ElementName = "ProductGroup")]
    [MaxLength(50)]
    public string? ProductGroup { get; set; }

    /// <summary>
    /// It shall correspond to the usual name of the goods or services provided, specifying the elements necessary to determine the applicable tax rate.
    /// 
    /// Max-length: 200
    /// Required
    /// </summary>
    [XmlElement(ElementName = "ProductDescription")]
    [MaxLength(200)]
    [Required]
    public required string ProductDescription { get; set; }

    /// <summary>
    /// The product’s EAN Code (bar code) shall be used.
    /// 
    /// If the EAN Code does not exist, fill in with the content of field 2.4.2. – ProductCode.
    /// 
    /// Max-length: 60
    /// Required
    /// </summary>
    [XmlElement(ElementName = "ProductNumberCode")]
    [MaxLength(200)]
    [Required]
    public required string ProductNumberCode { get; set; }
 
    /// <summary>
    /// TODO: Add documentation
    /// </summary>
    [XmlElement(ElementName = "CustomsDetails")]
    [MaxLength(50)]
    public CustomsDetails? CustomsDetails { get; set; }
}
