using System.Xml.Serialization;

namespace fiskaltrust.Middleware.Localization.QueuePT.Exports.SAFTPT.SAFTSchemaPT10401.SourceDocumentContracts;

[XmlRoot(ElementName = "OrderReferences")]
public class OrderReferences
{
    [XmlElement(ElementName = "OriginatingON")]
    public string? OriginatingON { get; set; }

    [XmlElement(ElementName = "OrderDate")]
    public DateTime? OrderDate { get; set; }
}
