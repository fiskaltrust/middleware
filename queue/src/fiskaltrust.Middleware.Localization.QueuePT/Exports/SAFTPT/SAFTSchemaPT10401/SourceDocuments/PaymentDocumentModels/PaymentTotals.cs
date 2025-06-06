﻿using System.Xml.Serialization;

namespace fiskaltrust.SAFT.CLI.SAFTSchemaPT10401;

[XmlRoot(ElementName = "DocumentTotals")]
public class PaymentTotals
{
    [XmlElement(ElementName = "TaxPayable")]
    public required decimal TaxPayable { get; set; }

    [XmlElement("NetTotal")]
    public required decimal NetTotal { get; set; }

    [XmlElement("GrossTotal")]
    public required decimal GrossTotal { get; set; }

    [XmlElement(ElementName = "Currency")]
    public Currency? Currency { get; set; }

    [XmlElement(ElementName = "Settlement")]
    public Settlement? Settlement { get; set; }
}



