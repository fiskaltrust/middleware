using System.Globalization;
using System.Xml.Serialization;

namespace fiskaltrust.Middleware.Localization.QueuePT.Logic.Exports.SAFTPT.SAFTSchemaPT10401.SourceDocuments.PaymentDocumentModels;
#pragma warning disable
[XmlRoot(ElementName = "Payment")]
public class PaymentDocument
{
    [XmlElement(ElementName = "PaymentRefNo")]
    public required string PaymentRefNo { get; set; }

    [XmlElement(ElementName = "ATCUD")]
    public required string ATCUD { get; set; }

    [XmlElement(ElementName = "Period")]
    public int? Period { get; set; }

    [XmlElement(ElementName = "TransactionID")]
    public string? TransactionID { get; set; }

    [XmlIgnore()]
    public required DateTime TransactionDate { get; set; }

    [XmlElement(ElementName = "TransactionDate")]
    public string TransactionDateString
    {
        get { return TransactionDate.ToString("yyyy-MM-dd"); }
        set { TransactionDate = DateTime.Parse(value); }
    }

    [XmlElement(ElementName = "PaymentType")]
    public required string PaymentType { get; set; }

    [XmlElement(ElementName = "Description")]
    public string? Description { get; set; }

    [XmlElement(ElementName = "SystemID")]
    public string? SystemID { get; set; }

    [XmlElement(ElementName = "DocumentStatus")]
    public required PaymentDocumentStatus DocumentStatus { get; set; }

    [XmlElement(ElementName = "PaymentMethod")]
    public required PaymentMethod PaymentMethod { get; set; }

    [XmlElement(ElementName = "SourceID")]
    public required string SourceID { get; set; }

    [XmlIgnore()]
    public required DateTime SystemEntryDate { get; set; }

#pragma warning disable
    [XmlElement(ElementName = "SystemEntryDate")]
    public string SystemEntryDateString
    {
        get { return SystemEntryDate.ToString("yyyy-MM-ddTHH:mm:ss"); }
        set { SystemEntryDate = DateTime.Parse(value); }
    }

    [XmlElement(ElementName = "CustomerID")]
    public required string CustomerID { get; set; }

    [XmlElement(ElementName = "Line")]
    public required List<PaymentLine> Line { get; set; }

    [XmlElement(ElementName = "DocumentTotals")]
    public PaymentTotals? DocumentTotals { get; set; }
}
