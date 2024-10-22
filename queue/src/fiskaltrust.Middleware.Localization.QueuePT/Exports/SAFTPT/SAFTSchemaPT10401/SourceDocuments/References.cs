using System.Xml.Serialization;

namespace fiskaltrust.SAFT.CLI.SAFTSchemaPT10401;

[XmlRoot(ElementName = "References")]
public class References
{
    [XmlElement(ElementName = "Reference")]
    public string? Reference { get; set; }

    [XmlElement(ElementName = "Reason")]
    public string? Reason { get; set; }
}