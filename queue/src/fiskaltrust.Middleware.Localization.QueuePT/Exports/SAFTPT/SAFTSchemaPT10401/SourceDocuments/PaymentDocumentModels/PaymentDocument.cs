using System.Globalization;
using System.Xml.Serialization;
using fiskaltrust.SAFT.CLI.SAFTSchemaPT10401;

namespace fiskaltrust.Middleware.Localization.QueuePT.Exports.SAFTPT.SAFTSchemaPT10401.SourceDocuments.PaymentDocumentModels;
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

    [XmlElement(ElementName = "WorkDate")]
    public string TransactionDateString
    {
        get { return TransactionDate.ToString("yyyy-MM-dd"); }
        set { TransactionDate = DateTime.Parse(value); }
    }

    [XmlElement(ElementName = "PaymentType")]
    public required string PaymentType { get; set; }

    [XmlElement(ElementName = "Description")]
    public required string Description { get; set; }

    [XmlElement(ElementName = "SystemID")]
    public required string SystemID { get; set; }

    [XmlElement(ElementName = "DocumentStatus")]
    public required PaymentDocumentStatus DocumentStatus { get; set; }

    [XmlElement(ElementName = "PaymentMethod")]
    public required PaymentMethod PaymentMethod { get; set; }

    [XmlElement(ElementName = "SourceID")]
    public required string SourceID { get; set; }

    [XmlElement(ElementName = "SystemEntryDate")]
    public required DateTime SystemEntryDate { get; set; }

    [XmlElement(ElementName = "CustomerID")]
    public required string CustomerID { get; set; }

    [XmlElement(ElementName = "Line")]
    public required List<Line> Line { get; set; }

    [XmlElement(ElementName = "DocumentTotals")]
    public PaymentSettlement? DocumentTotals { get; set; }
}
