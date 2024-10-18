using System.ComponentModel.DataAnnotations;
using System.Xml.Serialization;

namespace fiskaltrust.SAFT.CLI.SAFTSchemaPT10401;

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

