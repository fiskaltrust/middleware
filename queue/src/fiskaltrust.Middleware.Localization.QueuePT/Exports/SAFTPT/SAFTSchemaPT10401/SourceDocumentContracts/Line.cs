using System.Globalization;
using System.Xml.Serialization;

namespace fiskaltrust.Middleware.Localization.QueuePT.Exports.SAFTPT.SAFTSchemaPT10401.SourceDocumentContracts;

[XmlRoot(ElementName = "Line")]
public class Line
{
    [XmlElement(ElementName = "LineNumber")]
    public required long LineNumber { get; set; }

    [XmlElement(ElementName = "OrderReferences")]
    public OrderReferences? OrderReferences { get; set; }

    [XmlElement(ElementName = "ProductCode")]
    public required string ProductCode { get; set; }

    [XmlElement(ElementName = "ProductDescription")]
    public required string ProductDescription { get; set; }

    [XmlElement(ElementName = "Quantity")]
    public required decimal Quantity { get; set; }

    [XmlElement(ElementName = "UnitOfMeasure")]
    public required string UnitOfMeasure { get; set; }

    [XmlElement(ElementName = "UnitPrice")]
    public required decimal UnitPrice { get; set; }

    [XmlIgnore]
    public decimal? TaxBase { get; set; }

    [XmlElement("TaxBase", IsNullable = false)]
    public object? TaxBaseProperty
    {
        get => TaxBase;
        set
        {
            if (value != null && decimal.TryParse(value.ToString(), out var result))
            {
                TaxBase = result;
            }
            else
            {
                TaxBase = null;
            }
        }
    }

    [XmlIgnore]
    public required DateTime TaxPointDate { get; set; }

    [XmlElement(ElementName = "TaxPointDate")]
    public string TaxPointDateString
    {
        get => TaxPointDate.ToString("yyyy-MM-dd");
        set => TaxPointDate = DateTime.Parse(value);
    }

    [XmlElement(ElementName = "References")]
    public References? References { get; set; }

    [XmlElement(ElementName = "Description")]
    public required string Description { get; set; }

    [XmlElement(ElementName = "ProductSerialNumber")]
    public ProductSerialNumber? ProductSerialNumber { get; set; }

    [XmlIgnore]
    public decimal? DebitAmount { get; set; }

    [XmlElement("DebitAmount", IsNullable = false)]
    public object? DebitAmountProperty
    {
        get => DebitAmount;
        set
        {
            if(value != null && decimal.TryParse(value.ToString(), out var result))
            {
                DebitAmount = result;
            }
            else
            {
                DebitAmount = null;
            }
        }
    }

    [XmlElement(ElementName = "CreditAmount")]
    public required decimal CreditAmount { get; set; }

    [XmlElement(ElementName = "Tax")]
    public required Tax Tax { get; set; }

    [XmlElement(ElementName = "TaxExemptionReason")]
    public string? TaxExemptionReason { get; set; }

    [XmlElement(ElementName = "TaxExemptionCode")]
    public string? TaxExemptionCode { get; set; }

    [XmlIgnore]
    public decimal? SettlementAmount { get; set; }

    [XmlElement("SettlementAmount", IsNullable = false)]
    public string? SettlementAmountString
    {
        get => SettlementAmount?.ToString("F6", CultureInfo.InvariantCulture);
        set
        {
            if (value == null)
            {
                SettlementAmount = null;
            }
            else
            {
                SettlementAmount = decimal.Parse(value.ToString());
            }
        }
    }

    [XmlElement(ElementName = "CustomsInformation")]
    public CustomsInformation? CustomsInformation { get; set; }
}
