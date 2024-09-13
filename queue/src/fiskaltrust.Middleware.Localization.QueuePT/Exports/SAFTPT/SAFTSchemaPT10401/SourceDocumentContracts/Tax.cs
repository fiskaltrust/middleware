using System.Xml.Serialization;

namespace fiskaltrust.Middleware.Localization.QueuePT.Exports.SAFTPT.SAFTSchemaPT10401.SourceDocumentContracts;

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
    public object? TaxAmountProperty
    {
        get => TaxAmount;
        set
        {
            if (value != null && decimal.TryParse(value.ToString(), out var result))
            {
                TaxAmount = result;
            }
            else
            {
                TaxAmount = null;
            }
        }
    }
}

