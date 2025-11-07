using System.Xml.Serialization;

namespace fiskaltrust.Middleware.Localization.QueuePT.Logic.Exports.SAFTPT.SAFTSchemaPT10401.SourceDocuments;

[XmlRoot(ElementName = "DocumentStatus")]
public class InvoiceDocumentStatus
{
    [XmlElement(ElementName = "InvoiceStatus")]
    public required string InvoiceStatus { get; set; }


    [XmlIgnore()]
    public required DateTime InvoiceStatusDate { get; set; }

#pragma warning disable
    [XmlElement(ElementName = "InvoiceStatusDate")]
    public string InvoiceStatusDateString
    {
        get { return InvoiceStatusDate.ToString("yyyy-MM-ddTHH:mm:ss"); }
        set { InvoiceStatusDate = DateTime.Parse(value); }
    }

    [XmlElement(ElementName = "Reason")]
    public string? Reason { get; set; }

    [XmlElement(ElementName = "SourceID")]
    public required string SourceID { get; set; }

    [XmlElement(ElementName = "SourceBilling")]
    public required string SourceBilling { get; set; }
}

//*
// - sign is not allowed in the field;

