using System.Xml.Serialization;

namespace fiskaltrust.Middleware.Localization.QueuePT.Logic.Exports.SAFTPT.SAFTSchemaPT10401;

[XmlRoot(ElementName = "Settlement")]
public class Settlement
{
    [XmlElement(ElementName = "SettlementDiscount")]
    public string? SettlementDiscount { get; set; }

    [XmlElement(ElementName = "SettlementAmount")]
    public decimal? SettlementAmount { get; set; }
  
    [XmlElement(ElementName = "SettlementDate")]
    public DateTime? SettlementDate { get; set; }

    [XmlElement(ElementName = "PaymentTerms")]
    public string? PaymentTerms { get; set; }
}
