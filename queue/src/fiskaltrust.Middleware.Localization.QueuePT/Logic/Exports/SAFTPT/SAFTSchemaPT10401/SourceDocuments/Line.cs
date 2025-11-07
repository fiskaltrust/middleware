using System.Globalization;
using System.Xml.Serialization;

namespace fiskaltrust.Middleware.Localization.QueuePT.Logic.Exports.SAFTPT.SAFTSchemaPT10401;
#pragma warning disable
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

    [XmlIgnore()]
    public required decimal UnitPrice { get; set; }

    [XmlElement("UnitPrice", IsNullable = false)]
    public string UnitPriceProperty
    {
        get => UnitPrice.ToString("F6", CultureInfo.InvariantCulture);
        set => UnitPrice = decimal.Parse(value.ToString());
    }

    [XmlIgnore]
    public decimal? TaxBase { get; set; }

    [XmlElement("TaxBase", IsNullable = false)]
    public object TaxBaseProperty
    {
        get => TaxBase;
        set
        {
            if (value == null)
            {
                TaxBase = null;
            }
            else
            {
                TaxBase = decimal.Parse(value.ToString());
            }
        }
    }

    [XmlIgnore]
    public required DateTime TaxPointDate { get; set; }

    [XmlElement(ElementName = "TaxPointDate")]
    public string TaxPointDateString
    {
        get { return TaxPointDate.ToString("yyyy-MM-dd"); }
        set { TaxPointDate = DateTime.Parse(value); }
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
    public string DebitAmountProperty
    {
        get => DebitAmount?.ToString("F6", CultureInfo.InvariantCulture);
        set
        {
            if (value == null)
            {
                DebitAmount = null;
            }
            else
            {
                DebitAmount = decimal.Parse(value.ToString());
            }
        }
    }

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
