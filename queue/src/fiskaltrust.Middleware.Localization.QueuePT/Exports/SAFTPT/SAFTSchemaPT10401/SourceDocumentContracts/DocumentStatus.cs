using System.Xml.Serialization;

namespace fiskaltrust.Middleware.Localization.QueuePT.Exports.SAFTPT.SAFTSchemaPT10401.SourceDocumentContracts;

[XmlRoot(ElementName = "DocumentStatus")]
public class DocumentStatus
{
    [XmlElement(ElementName = "InvoiceStatus")]
    public required string InvoiceStatus { get; set; }

    [XmlElement(ElementName = "InvoiceStatusDate")]
    public required DateTime InvoiceStatusDate { get; set; }

    [XmlElement(ElementName = "Reason")]
    public string? Reason { get; set; }

    [XmlElement(ElementName = "SourceID")]
    public required string SourceID { get; set; }

    [XmlElement(ElementName = "SourceBilling")]
    public required string SourceBilling { get; set; }
}

