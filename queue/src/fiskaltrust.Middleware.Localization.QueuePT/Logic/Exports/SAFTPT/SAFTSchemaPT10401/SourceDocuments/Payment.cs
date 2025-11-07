using System.Globalization;
using System.Xml.Serialization;

namespace fiskaltrust.Middleware.Localization.QueuePT.Logic.Exports.SAFTPT.SAFTSchemaPT10401.SourceDocuments;
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
