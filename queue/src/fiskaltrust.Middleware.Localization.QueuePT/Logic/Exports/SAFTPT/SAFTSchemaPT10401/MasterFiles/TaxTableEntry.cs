using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Xml.Serialization;

namespace fiskaltrust.Middleware.Localization.QueuePT.Logic.Exports.SAFTPT.SAFTSchemaPT10401;
#pragma warning disable
[XmlRoot(ElementName = "TaxTableEntry")]
public class TaxTableEntry
{
    /// <summary>
    /// This field shall be filled in with the tax type:
    /// "IVA" – Value Added Tax;
    /// "IS" – Stamp Duty;
    /// "NS" – Not subject to VAT or Stamp Duty.
    /// 
    /// Max-length: 3
    /// Required
    /// </summary>
    [XmlElement(ElementName = "TaxType")]
    [MaxLength(3)]
    [Required]
    public required string TaxType { get; set; }

    /// <summary>
    /// This field must be filled in with the norm ISO 3166-1-alpha-2.
    /// 
    /// In the case of the Autonomous Regions of the Azores and Madeira Island the field must be filled in with:
    /// 
    /// - "PT-AC" – Fiscal area of the Autonomous Region of the Azores;
    /// - "PT-MA" – Fiscal area of the Autonomous Region of the Madeira Island.
    /// 
    /// Max-length: 5
    /// Required
    /// </summary>
    [XmlElement(ElementName = "TaxCountryRegion")]
    [MaxLength(5)]
    [Required]
    public required string TaxCountryRegion { get; set; }

    /// <summary>
    /// In case field 2.5.1.1. – TaxType = IVA, the field must be filled in with:
    /// 
    /// "RED" – Reduced tax rate;
    /// "INT" – Intermediate tax rate;
    /// "NOR" – Normal tax rate;
    /// "ISE" – Exempted;
    /// "OUT" – Others, applicable to the special VAT regimes.
    /// 
    /// In case field 2.5.1.1. – TaxType = IS, it shall be filled in with:
    /// - The correspondent code of the Stamp Duty’s table;
    /// - "ISE" – Exempted.
    /// 
    /// In case it is not subject to tax it shall be filled in with "NS".
    /// 
    /// In receipts issued without tax discriminated it shall be filled in with "NA".
    /// 
    /// Max-length: 10
    /// Required
    /// </summary>
    [XmlElement(ElementName = "TaxCode")]
    [MaxLength(10)]
    [Required]
    public required string TaxCode { get; set; }

    /// <summary>
    /// In the case of Stamp Duty, the field shall be filled in with the respective table code description.
    /// 
    /// Max-length: 255
    /// Required
    /// </summary>
    [XmlElement(ElementName = "Description")]
    [MaxLength(10)]
    [Required]
    public required string Description { get; set; }

    /// <summary>
    /// The last legal date to apply the tax rate, in the case of alteration of the same, at the time of the taxation period in force.
    /// </summary>
    [XmlIgnore]
    public DateTime? TaxExpirationDate { get; set; }

     [XmlElement("TaxExpirationDate", IsNullable = false)]
     public object TaxExpirationDatetProperty    {
        get
        {
            return TaxExpirationDate;
        }
        set
        {
            if (value == null)
            {
                TaxExpirationDate = null;
            }
            else if (value is DateTime || value is DateTime?)
            {
                TaxExpirationDate = (DateTime)value;
            }
            else
            {
                TaxExpirationDate = DateTime.Parse(value.ToString());
            }
        }
    }
    /// <summary>
    /// It is required to fill in this field, if we are dealing with a tax percentage.
    /// 
    /// In case of exemption or not subject to tax, fill in with “0” (zero).
    /// 
    /// Percentage
    /// Required
    /// </summary>
    [XmlIgnore]
    public decimal? TaxPercentage { get; set; }

    [XmlElement("TaxPercentage", IsNullable = false)]
    public string TaxPercentageString
    {
        get => TaxPercentage?.ToString("F6", CultureInfo.InvariantCulture);
        set
        {
            if (value == null)
            {
                TaxPercentage = null;
            }
            else
            {
                TaxPercentage = decimal.Parse(value.ToString());
            }
        }
    }

    /// <summary>
    /// It is required to fill in this field, if it is a fixed stamp duty amount.
    /// 
    /// Monetary
    /// Required
    /// </summary>
    [XmlIgnore()]
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

