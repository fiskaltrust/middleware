using System.Xml.Serialization;

namespace fiskaltrust.Middleware.Localization.QueuePT.Logic.Exports.SAFTPT.SAFTSchemaPT10401.SourceDocuments.PaymentDocumentModels;

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