using System.Xml.Serialization;

namespace fiskaltrust.Middleware.Localization.QueuePT.Logic.Exports.SAFTPT.SAFTSchemaPT10401.SourceDocuments;

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
