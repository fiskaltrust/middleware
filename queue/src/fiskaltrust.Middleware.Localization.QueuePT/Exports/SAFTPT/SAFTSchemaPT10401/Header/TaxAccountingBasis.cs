using System.Xml.Serialization;

namespace fiskaltrust.SAFT.CLI.SAFTSchemaPT10401;

public enum TaxAccountingBasis
{
      [XmlEnum(Name="C")]
      Accounting,
      [XmlEnum(Name="E")]
      InvoicesIssuedByThirdParties,
      [XmlEnum(Name="F")]
      Invoicing,
      [XmlEnum(Name="I")]
      InvoicingAndAccountingIntegratedData,
      [XmlEnum(Name="P")]
      InvoicingPartialData,
      [XmlEnum(Name="R")]
      Receipts,
      [XmlEnum(Name="S")]
      SelfBilling,
      [XmlEnum(Name="T")]
      TransportDocuments
}

