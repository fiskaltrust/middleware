using System.Xml.Serialization;

namespace fiskaltrust.SAFT.CLI.SAFTSchemaPT10401;

[XmlRoot(ElementName = "CustomsInformation")]
public class CustomsInformation
{
    [XmlElement(ElementName = "ARCNo")]
    public string? ARCNo { get; set; }

    [XmlElement(ElementName = "IECAmount")]
    public decimal? IECAmount { get; set; }
}
