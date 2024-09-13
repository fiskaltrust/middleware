using System.Xml.Serialization;

namespace fiskaltrust.Middleware.Localization.QueuePT.Exports.SAFTPT.SAFTSchemaPT10401.SourceDocumentContracts;

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
    public string? PaymentDateString
    {
        get => PaymentDate?.ToString("yyyy-MM-dd");
        set
        {
            if(value != null && DateTime.TryParse(value, out var result))
            {
                PaymentDate = result;
            }
            else
            {
                PaymentDate = null;
            }
        }
    }
}
