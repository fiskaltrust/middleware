using System.Xml.Serialization;

namespace fiskaltrust.Middleware.Localization.QueuePT.Exports.SAFTPT.SAFTSchemaPT10401.SourceDocumentContracts;

[XmlRoot(ElementName = "ProductSerialNumber")]
public class ProductSerialNumber
{
    [XmlElement(ElementName = "SerialNumber")]
    public required string SerialNumber { get; set; }
}
