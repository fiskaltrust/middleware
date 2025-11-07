using System.Xml.Serialization;

namespace fiskaltrust.Middleware.Localization.QueuePT.Logic.Exports.SAFTPT.SAFTSchemaPT10401.SourceDocuments;
#pragma warning disable
[XmlRoot(ElementName = "Invoice")]
public class Invoice
{
    /// <summary>
    /// It is made of the document type internal code, followed by a space, followed by the identifier of the document series, followed by (/) and by a sequential number of the document within the series.
    /// 
    /// In this field cannot exist records with the same identification.
    /// 
    /// The same document type internal code cannot be used for different types of documents.
    /// </summary>
    [XmlElement(ElementName = "InvoiceNo")]
    public required string InvoiceNo { get; set; }

    [XmlElement(ElementName = "ATCUD")]
    public required string ATCUD { get; set; }

    [XmlElement(ElementName = "DocumentStatus")]
    public required InvoiceDocumentStatus DocumentStatus { get; set; }

    [XmlElement(ElementName = "Hash")]
    public required string Hash { get; set; }

    [XmlElement(ElementName = "HashControl")]
    public required string HashControl { get; set; }

    [XmlElement(ElementName = "Period")]
    public int? Period { get; set; }

    [XmlIgnore()]
    public required DateTime InvoiceDate { get; set; }

    [XmlElement(ElementName = "InvoiceDate")]
    public string InvoiceDateString
    {
        get { return InvoiceDate.ToString("yyyy-MM-dd"); }
        set { InvoiceDate = DateTime.Parse(value); }
    }

    [XmlElement(ElementName = "InvoiceType")]
    public required  string InvoiceType { get; set; }

    [XmlElement(ElementName = "SpecialRegimes")]
    public required SpecialRegimes SpecialRegimes { get; set; }

    [XmlElement(ElementName = "SourceID")]
    public required string SourceID { get; set; }

    [XmlElement(ElementName = "EACCode")]
    public string? EACCode { get; set; }

    [XmlIgnore()]
    public required DateTime SystemEntryDate { get; set; }

#pragma warning disable
    [XmlElement(ElementName = "SystemEntryDate")]
    public string SystemEntryDateString
    {
        get { return SystemEntryDate.ToString("yyyy-MM-ddTHH:mm:ss"); }
        set { SystemEntryDate = DateTime.Parse(value); }
    }

    [XmlElement(ElementName = "TransactionID")]
    public string? TransactionID { get; set; }

    [XmlElement(ElementName = "CustomerID")]
    public required string CustomerID { get; set; }

    [XmlElement(ElementName = "ShipTo")]
    public ShipTo? ShipTo { get; set; }

    [XmlElement(ElementName = "ShipFrom")]
    public ShipFrom? ShipFrom { get; set; }

    [XmlIgnore]
    public DateTime? MovementStartTime { get; set; }

    [XmlElement("MovementStartTime", IsNullable = false)]
    public object MovementStartTimeProperty
    {
        get
        {
            return MovementStartTime;
        }
        set
        {
            if (value == null)
            {
                MovementStartTime = null;
            }
            else if (value is DateTime || value is DateTime?)
            {
                MovementStartTime = (DateTime)value;
            }
            else
            {
                MovementStartTime = DateTime.Parse(value.ToString());
            }
        }
    }

    [XmlElement(ElementName = "Line")]
    public required List<Line> Line { get; set; }

    [XmlElement(ElementName = "DocumentTotals")]
    public DocumentTotals? DocumentTotals { get; set; }

    [XmlElement(ElementName = "WithholdingTax")]
    public WithholdingTax? WithholdingTax { get; set; }
}

