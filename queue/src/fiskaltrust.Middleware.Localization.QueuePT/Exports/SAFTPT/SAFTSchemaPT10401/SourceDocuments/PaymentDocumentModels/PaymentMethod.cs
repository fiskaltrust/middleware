using System.Globalization;
using System.Xml.Serialization;

namespace fiskaltrust.Middleware.Localization.QueuePT.Exports.SAFTPT.SAFTSchemaPT10401.SourceDocuments.PaymentDocumentModels;

[XmlRoot(ElementName = "PaymentMethod")]
public class PaymentMethod
{
    [XmlElement(ElementName = "PaymentMechanism")]
    public string? PaymentMechanism { get; set; }

    [XmlIgnore]
    public required decimal PaymentAmount { get; set; }

    [XmlElement("PaymentAmount", IsNullable = false)]
    public string PaymentAmountProperty
    {
        get => PaymentAmount.ToString("F6", CultureInfo.InvariantCulture);
        set => PaymentAmount = decimal.Parse(value.ToString());
    }

    [XmlIgnore()]
    public required DateTime PaymentDate { get; set; }

#pragma warning disable
    [XmlElement(ElementName = "PaymentDate")]
    public string PaymentDateeString
    {
        get { return PaymentDate.ToString("yyyy-MM-dd"); }
        set { PaymentDate = DateTime.Parse(value); }
    }
}
