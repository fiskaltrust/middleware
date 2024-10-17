using System.Xml.Serialization;

namespace fiskaltrust.Middleware.Localization.QueuePT.Exports.SAFTPT.SAFTSchemaPT10401.SourceDocumentContracts;

[XmlRoot(ElementName = "Currency")]
public class Currency
{
    [XmlElement(ElementName = "CurrencyCode")]
    public string? CurrencyCode { get; set; }

    [XmlElement(ElementName = "CurrencyAmount")]
    public decimal? CurrencyAmount { get; set; }

    [XmlElement(ElementName = "ExchangeRate")]
    public decimal? ExchangeRate { get; set; }
}
