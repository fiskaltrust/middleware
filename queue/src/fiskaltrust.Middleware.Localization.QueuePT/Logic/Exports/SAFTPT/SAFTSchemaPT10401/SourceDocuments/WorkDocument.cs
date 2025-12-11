using System.Xml.Serialization;

namespace fiskaltrust.Middleware.Localization.QueuePT.Logic.Exports.SAFTPT.SAFTSchemaPT10401;
#pragma warning disable
[XmlRoot(ElementName = "WorkDocument")]
public class WorkDocument
{
    [XmlElement(ElementName = "DocumentNumber")]
    public required string DocumentNumber { get; set; }

    [XmlElement(ElementName = "ATCUD")]
    public required string ATCUD { get; set; }

    [XmlElement(ElementName = "DocumentStatus")]
    public required WorkDocumentStatus DocumentStatus { get; set; }

    [XmlElement(ElementName = "Hash")]
    public required string Hash { get; set; }

    [XmlElement(ElementName = "HashControl")]
    public required int HashControl { get; set; }

    [XmlElement(ElementName = "Period")]
    public int? Period { get; set; }

    [XmlIgnore()]
    public required DateTime WorkDate { get; set; }

    [XmlElement(ElementName = "WorkDate")]
    public string WorkDateString
    {
        get { return WorkDate.ToString("yyyy-MM-dd"); }
        set { WorkDate = DateTime.Parse(value); }
    }

    [XmlElement(ElementName = "WorkType")]
    public required  string WorkType { get; set; }

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

    [XmlElement(ElementName = "TransactionID")]
    public string? TransactionID { get; set; }

    [XmlElement(ElementName = "CustomerID")]
    public required string CustomerID { get; set; }

    [XmlElement(ElementName = "Line")]
    public required List<Line> Line { get; set; }

    [XmlElement(ElementName = "DocumentTotals")]
    public WorkDocumentTotals? DocumentTotals { get; set; }
}

