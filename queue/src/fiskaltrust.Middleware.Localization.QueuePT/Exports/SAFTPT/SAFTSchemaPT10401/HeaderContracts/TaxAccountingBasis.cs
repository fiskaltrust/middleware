using System.Xml.Serialization;

namespace fiskaltrust.Middleware.Localization.QueuePT.Exports.SAFTPT.SAFTSchemaPT10401.HeaderContracts;

public enum TaxAccountingBasis
{
    [XmlEnum(Name = "C")]
    Accounting,
    [XmlEnum(Name = "E")]
    InvoicesIssuedByThirdParties,
    [XmlEnum(Name = "F")]
    Invoicing,
    [XmlEnum(Name = "I")]
    InvoicingAndAccountingIntegratedData,
    [XmlEnum(Name = "P")]
    InvoicingPartialData,
    [XmlEnum(Name = "R")]
    Receipts,
    [XmlEnum(Name = "S")]
    SelfBilling,
    [XmlEnum(Name = "T")]
    TransportDocuments
}

