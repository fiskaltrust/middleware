using System.Xml.Serialization;
using fiskaltrust.Middleware.Localization.QueuePT.Logic.Exports.SAFTPT.SAFTSchemaPT10401.SourceDocuments;

namespace fiskaltrust.Middleware.Localization.QueuePT.Logic.Exports.SAFTPT.SAFTSchemaPT10401.SourceDocuments.PaymentDocumentModels;

[XmlRoot(ElementName = "DocumentTotals")]
public class PaymentTotals
{
    [XmlElement(ElementName = "TaxPayable")]
    public required decimal TaxPayable { get; set; }

    [XmlElement("NetTotal")]
    public required decimal NetTotal { get; set; }

    [XmlElement("GrossTotal")]
    public required decimal GrossTotal { get; set; }

    [XmlElement(ElementName = "Currency")]
    public Currency? Currency { get; set; }

    [XmlElement(ElementName = "Settlement")]
    public Settlement? Settlement { get; set; }
}



