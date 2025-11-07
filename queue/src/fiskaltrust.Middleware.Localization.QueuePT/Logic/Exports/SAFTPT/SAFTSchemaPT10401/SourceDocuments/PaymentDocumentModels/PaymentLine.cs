using System.Globalization;
using System.Xml.Serialization;

namespace fiskaltrust.Middleware.Localization.QueuePT.Logic.Exports.SAFTPT.SAFTSchemaPT10401.SourceDocuments.PaymentDocumentModels;
#pragma warning disable
[XmlRoot(ElementName = "Line")]
public class PaymentLine
{
    [XmlElement(ElementName = "LineNumber")]
    public required long LineNumber { get; set; }

    [XmlElement(ElementName = "SourceDocumentID")]
    public SourceDocument? SourceDocumentID { get; set; }

    [XmlIgnore]
    public decimal? CreditAmount { get; set; }

    [XmlElement("CreditAmount", IsNullable = false)]
    public string CreditAmountProperty
    {
        get => CreditAmount?.ToString("F6", CultureInfo.InvariantCulture);
        set
        {
            if (value == null)
            {
                CreditAmount = null;
            }
            else
            {
                CreditAmount = decimal.Parse(value.ToString());
            }
        }
    }
}
