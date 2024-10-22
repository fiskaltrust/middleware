using System.Xml.Serialization;

namespace fiskaltrust.SAFT.CLI.SAFTSchemaPT10401;

[XmlRoot(ElementName = "OrderReferences")]
public class OrderReferences
{
    [XmlElement(ElementName = "OriginatingON")]
    public string? OriginatingON { get; set; }

    [XmlElement(ElementName = "OrderDate")]
    public DateTime? OrderDate { get; set; }
}
