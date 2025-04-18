using System.Xml.Serialization;

namespace fiskaltrust.SAFT.CLI.SAFTSchemaPT10401;

[XmlRoot(ElementName = "OrderReferences")]
public class OrderReferences
{
    [XmlElement(ElementName = "OriginatingON")]
    public string? OriginatingON { get; set; }

    [XmlIgnore()]
    public required DateTime OrderDate { get; set; }

#pragma warning disable
    [XmlElement(ElementName = "OrderDate")]
    public string OrderDateString
    {
        get { return OrderDate.ToString("yyyy-MM-dd"); }
        set { OrderDate = DateTime.Parse(value); }
    }
}
