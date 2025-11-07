using System.Xml.Serialization;

namespace fiskaltrust.Middleware.Localization.QueuePT.Logic.Exports.SAFTPT.SAFTSchemaPT10401;

[XmlRoot(ElementName = "DocumentStatus")]
public class WorkDocumentStatus
{
    [XmlElement(ElementName = "WorkStatus")]
    public required string WorkStatus { get; set; }


    [XmlIgnore()]
    public required DateTime WorkStatusDate { get; set; }

#pragma warning disable
    [XmlElement(ElementName = "WorkStatusDate")]
    public string WorkStatusDateString
    {
        get { return WorkStatusDate.ToString("yyyy-MM-ddTHH:mm:ss"); }
        set { WorkStatusDate = DateTime.Parse(value); }
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