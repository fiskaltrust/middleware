using System.Globalization;
using System.Xml.Serialization;

namespace fiskaltrust.SAFT.CLI.SAFTSchemaPT10401;

[XmlRoot(ElementName = "DocumentTotals")]
public class DocumentTotals
{
    [XmlIgnore()]
    public required decimal TaxPayable { get; set; }

    [XmlElement("TaxPayable", IsNullable = false)]
    public string TaxPayableProperty
    {
        get => TaxPayable.ToString("F2", CultureInfo.InvariantCulture);
        set => TaxPayable = decimal.Parse(value.ToString());
    }

    [XmlIgnore()]
    public required decimal NetTotal { get; set; }

    [XmlElement("NetTotal", IsNullable = false)]
    public string NetTotalProperty
    {
        get => NetTotal.ToString("F2", CultureInfo.InvariantCulture);
        set => NetTotal = decimal.Parse(value.ToString());
    }

    [XmlIgnore()]
    public required decimal GrossTotal { get; set; }

    [XmlElement("GrossTotal", IsNullable = false)]
    public string GrossTotalProperty
    {
        get => GrossTotal.ToString("F2", CultureInfo.InvariantCulture);
        set => GrossTotal = decimal.Parse(value.ToString());
    }

    [XmlElement(ElementName = "Currency")]
    public Currency? Currency { get; set; }

    [XmlElement(ElementName = "Settlement")]
    public Settlement? Settlement { get; set; }

    [XmlElement(ElementName = "Payment")]
    public List<Payment>? Payment { get; set; }
}



