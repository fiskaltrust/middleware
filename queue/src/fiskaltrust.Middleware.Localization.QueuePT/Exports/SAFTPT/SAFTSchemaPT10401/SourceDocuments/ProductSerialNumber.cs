using System.Xml.Serialization;

namespace fiskaltrust.SAFT.CLI.SAFTSchemaPT10401;

[XmlRoot(ElementName = "ProductSerialNumber")]
public class ProductSerialNumber
{
    [XmlElement(ElementName = "SerialNumber")]
    public required string SerialNumber { get; set; }
}
