using System.Xml.Serialization;

namespace fiskaltrust.Middleware.Localization.QueuePT.Logic.Exports.SAFTPT.SAFTSchemaPT10401.SourceDocuments;
#pragma warning disable
[XmlRoot(ElementName = "Tax")]
public class Tax
{
    [XmlElement(ElementName = "TaxType")]
    public required string TaxType { get; set; }

    [XmlElement(ElementName = "TaxCountryRegion")]
    public required string TaxCountryRegion { get; set; }

    [XmlElement(ElementName = "TaxCode")]
    public required string TaxCode { get; set; }

    [XmlElement(ElementName = "TaxPercentage")]
    public decimal? TaxPercentage { get; set; }

    [XmlIgnore]
    public decimal? TaxAmount { get; set; }

    [XmlElement("TaxAmount", IsNullable = false)]
    public object TaxAmountProperty
    {
        get => TaxAmount;
        set
        {
            if (value == null)
            {
                TaxAmount = null;
            }
            else
            {
                TaxAmount = decimal.Parse(value.ToString());
            }
        }
    }
}

