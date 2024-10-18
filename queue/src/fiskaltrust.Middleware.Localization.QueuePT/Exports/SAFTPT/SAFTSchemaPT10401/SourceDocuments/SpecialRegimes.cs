using System.Xml.Serialization;

namespace fiskaltrust.SAFT.CLI.SAFTSchemaPT10401;

[XmlRoot(ElementName = "SpecialRegimes")]
public class SpecialRegimes
{

    [XmlElement(ElementName = "SelfBillingIndicator")]
    public required int SelfBillingIndicator { get; set; }

    [XmlElement(ElementName = "CashVATSchemeIndicator")]
    public required int CashVATSchemeIndicator { get; set; }

    [XmlElement(ElementName = "ThirdPartiesBillingIndicator")]
    public required int ThirdPartiesBillingIndicator { get; set; }
}

