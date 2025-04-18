using System.Xml.Serialization;

namespace fiskaltrust.SAFT.CLI.SAFTSchemaPT10401;

[XmlRoot(ElementName = "SourceDocumentID")]
public class SourceDocument
{
    [XmlElement(ElementName = "OriginatingON")]
    public string? OriginatingON { get; set; }

    [XmlIgnore()]
    public required DateTime InvoiceDate { get; set; }

#pragma warning disable
    [XmlElement(ElementName = "InvoiceDate")]
    public string InvoiceDateString
    {
        get { return InvoiceDate.ToString("yyyy-MM-dd"); }
        set { InvoiceDate = DateTime.Parse(value); }
    }
}