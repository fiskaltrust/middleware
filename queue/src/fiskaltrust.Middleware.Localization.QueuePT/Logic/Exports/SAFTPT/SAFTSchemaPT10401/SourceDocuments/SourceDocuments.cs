using System.Xml.Serialization;
using fiskaltrust.Middleware.Localization.QueuePT.Logic.Exports.SAFTPT.SAFTSchemaPT10401.PaymentDocumentModels;

namespace fiskaltrust.Middleware.Localization.QueuePT.Logic.Exports.SAFTPT.SAFTSchemaPT10401;

[XmlRoot(ElementName = "SourceDocuments")]
public class SourceDocuments
{
    /// <summary>
    /// This table shall present all sales documents and correcting documents issued by the company, including cancelled documents, duly marked, enabling a verification of the documents’ numbering sequence within each documental series, which should have an annual numbering at least.
    /// 
    /// Type of documents to be exported: all documents mentioned in field 4.1.4.8. – InvoiceType
    /// </summary>
    [XmlElement(ElementName = "SalesInvoices")]
    public SalesInvoices? SalesInvoices { get; set; }

    /// <summary>
    /// MovementOfGoods
    /// </summary>
    //[XmlElement(ElementName = "MovementOfGoods")]
    //public object? MovementOfGoods { get; set; }

    /// <summary>
    /// In this table shall be exported any other documents issued, apart from its designation, likely to be presented to the costumer for the purpose of checking goods or provision of services, even when subject to later invoicing.
    /// 
    /// This table shall not include the documents required to be exported in Tables 4.1. – SalesInvoices or 4.2 – MovementOfGoods.
    /// </summary>
    [XmlElement(ElementName = "WorkingDocuments")]
    public WorkingDocuments? WorkingDocuments { get; set; }

    /// <summary>
    /// Receipts issued after the entry into force of this structure should be exported on this table.
    /// </summary>
    [XmlElement(ElementName = "Payments")]
    public Payments? Payments { get; set; }
}

