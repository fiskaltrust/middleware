using System.Xml.Serialization;

namespace fiskaltrust.SAFT.CLI.SAFTSchemaPT10401;

[XmlRoot(ElementName = "Settlement")]
public class PaymentSettlement
{
    [XmlElement(ElementName = "SettlementAmount")]
    public decimal? SettlementAmount { get; set; }
}
