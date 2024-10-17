using System.Xml.Serialization;

namespace fiskaltrust.Middleware.Localization.QueuePT.Exports.SAFTPT.SAFTSchemaPT10401.SourceDocumentContracts;

[XmlRoot(ElementName = "CustomsInformation")]
public class CustomsInformation
{
    [XmlElement(ElementName = "ARCNo")]
    public string? ARCNo { get; set; }

    [XmlElement(ElementName = "IECAmount")]
    public decimal? IECAmount { get; set; }
}
