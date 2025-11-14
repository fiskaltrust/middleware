using System.Xml.Serialization;

namespace fiskaltrust.Middleware.Localization.QueuePT.Logic.Exports.SAFTPT.SAFTSchemaPT10401.PaymentDocumentModels;

[XmlRoot(ElementName = "Settlement")]
public class PaymentSettlement
{
    [XmlElement(ElementName = "SettlementAmount")]
    public decimal? SettlementAmount { get; set; }
}
