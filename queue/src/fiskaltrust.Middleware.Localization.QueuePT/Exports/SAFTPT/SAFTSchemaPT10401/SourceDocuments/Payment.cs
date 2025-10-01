using System.Globalization;
using System.Xml.Serialization;

namespace fiskaltrust.SAFT.CLI.SAFTSchemaPT10401;
#pragma warning disable
[XmlRoot(ElementName = "Payment")]
public class Payment
{
    [XmlElement(ElementName = "PaymentMechanism")]
    public string? PaymentMechanism { get; set; }

    [XmlIgnore]
    public required decimal PaymentAmount { get; set; }

    [XmlElement("PaymentAmount", IsNullable = false)]
    public string PaymentAmountProperty
    {
        get => PaymentAmount.ToString("F2", CultureInfo.InvariantCulture);
        set => PaymentAmount = decimal.Parse(value.ToString());
    }

    [XmlIgnore]
    public DateTime? PaymentDate { get; set; }

    [XmlElement(ElementName = "PaymentDate")]
    public string PaymentDateString
    {
        get { return PaymentDate?.ToString("yyyy-MM-dd"); }
        set { PaymentDate = DateTime.Parse(value); }
    }
}
