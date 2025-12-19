using System.ComponentModel.DataAnnotations;
using System.Xml.Serialization;

namespace fiskaltrust.Middleware.Localization.QueuePT.Logic.Exports.SAFTPT.SAFTSchemaPT10401;

[XmlRoot(ElementName = "CustomsDetails")]
public class CustomsDetails
{

    /// <summary>
    /// Fill in with the European Union Combined Nomenclature code.
    /// 
    /// If there is a need to make more than one reference, this field can be generated as many times as necessary.
    /// 
    /// Max-length 8
    /// </summary>
    [XmlElement(ElementName = "CNCode")]
    [MaxLength(8)]
    public string? CNCode { get; set; }

    /// <summary>
    /// Fill in with the UN [United Nations] number for dangerous products.
    /// If there is a need to make more than one reference, this field can be generated as many times as necessary.
    /// 
    /// Max-length 4
    /// </summary>
    [XmlElement(ElementName = "UNNumber")]
    [MaxLength(4)]
    public string? UNNumber { get; set; }
}