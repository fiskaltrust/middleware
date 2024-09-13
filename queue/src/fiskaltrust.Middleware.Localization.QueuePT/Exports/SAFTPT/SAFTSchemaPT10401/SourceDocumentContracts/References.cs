using System.Xml.Serialization;

namespace fiskaltrust.Middleware.Localization.QueuePT.Exports.SAFTPT.SAFTSchemaPT10401.SourceDocumentContracts;

[XmlRoot(ElementName = "References")]
public class References
{
    [XmlElement(ElementName = "Reference")]
    public string? Reference { get; set; }

    [XmlElement(ElementName = "Reason")]
    public string? Reason { get; set; }
}