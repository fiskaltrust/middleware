using System.Xml.Serialization;

namespace fiskaltrust.SAFT.CLI.SAFTSchemaPT10401;

[XmlRoot(ElementName = "WithholdingTax")]
public class WithholdingTax
{
    [XmlElement(ElementName = "WithholdingTaxType")]
    public string? WithholdingTaxType { get; set; }

    [XmlElement(ElementName = "WithholdingTaxDescription")]
    public string? WithholdingTaxDescription { get; set; }

    [XmlElement(ElementName = "WithholdingTaxAmount")]
    public required decimal WithholdingTaxAmount { get; set; }
}

