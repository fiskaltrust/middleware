using System.Xml.Serialization;

namespace fiskaltrust.SAFT.CLI.SAFTSchemaPT10401;

[XmlRoot(ElementName = "DocumentStatus")]
public class PaymentDocumentStatus
{
    [XmlElement(ElementName = "PaymentStatus")]
    public required string PaymentStatus { get; set; }


    [XmlIgnore()]
    public required DateTime PaymentStatusDate { get; set; }

#pragma warning disable
    [XmlElement(ElementName = "PaymentStatusDate")]
    public string PaymentStatusDateString
    {
        get { return PaymentStatusDate.ToString("yyyy-MM-ddTHH:mm:ss"); }
        set { PaymentStatusDate = DateTime.Parse(value); }
    }

    [XmlElement(ElementName = "Reason")]
    public string? Reason { get; set; }

    [XmlElement(ElementName = "SourceID")]
    public required string SourceID { get; set; }

    [XmlElement(ElementName = "SourcePayment")]
    public required string SourcePayment { get; set; }
}

//*
// - sign is not allowed in the field;

