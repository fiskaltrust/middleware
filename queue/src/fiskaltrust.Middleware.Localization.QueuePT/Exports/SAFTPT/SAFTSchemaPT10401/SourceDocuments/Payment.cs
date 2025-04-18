using System.Xml.Serialization;

namespace fiskaltrust.SAFT.CLI.SAFTSchemaPT10401;
#pragma warning disable
[XmlRoot(ElementName = "Payment")]
public class Payment
{
    [XmlElement(ElementName = "PaymentMechanism")]
    public string? PaymentMechanism { get; set; }

    [XmlElement(ElementName = "PaymentAmount")]
    public decimal? PaymentAmount { get; set; }
  
    [XmlIgnore]
    public DateTime? PaymentDate { get; set; }

    [XmlElement(ElementName = "PaymentDate")]
    public string PaymentDateString
    {
        get { return PaymentDate?.ToString("yyyy-MM-dd"); }
        set { PaymentDate = DateTime.Parse(value); }
    }
}
