using System.ComponentModel.DataAnnotations;
using System.Xml.Serialization;

namespace fiskaltrust.Middleware.Localization.QueuePT.Exports.SAFTPT.SAFTSchemaPT10401.MasterFileContracts;


[XmlRoot(ElementName = "TaxTable")]
public class TaxTable
{
    /// <summary>
    /// Tax Table record
    /// </summary>
    [XmlElement(ElementName = "TaxTableEntry")]
    [Required]
    public required List<TaxTableEntry> TaxTableEntry { get; set; }
}

