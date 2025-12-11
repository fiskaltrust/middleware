using System.Xml.Serialization;

namespace fiskaltrust.Middleware.Localization.QueuePT.Logic.Exports.SAFTPT.SAFTSchemaPT10401;

[XmlRoot(ElementName = "DocumentTotals")]
public class WorkDocumentTotals
{
    [XmlElement(ElementName = "TaxPayable")]
    public required decimal TaxPayable { get; set; }

    [XmlElement("NetTotal")]
    public required decimal NetTotal { get; set; }

    [XmlElement("GrossTotal")]
    public required decimal GrossTotal { get; set; }

    [XmlElement(ElementName = "Currency")]
    public Currency? Currency { get; set; }
}



