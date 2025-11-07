using System.ComponentModel.DataAnnotations;
using System.Xml.Serialization;

namespace fiskaltrust.Middleware.Localization.QueuePT.Logic.Exports.SAFTPT.SAFTSchemaPT10401.PaymentDocumentModels;

[XmlRoot(ElementName = "Payments")]
public class Payments
{
    /// <summary>
    /// The field shall contain the total number of documents, including the documents which content in field 4.1.4.3.1. - InvoiceStatus is "A" or "F".
    /// 
    /// int
    /// Required
    /// </summary>
    [XmlElement(ElementName = "NumberOfEntries")]
    [Required]
    public required int NumberOfEntries { get; set; }

    /// <summary>
    /// The field shall contain the control sum of field 4.1.4.19.13. - DebitAmount, excluding the documents which content in field 4.1.4.3.1. - InvoiceStatus is "A" or "F".
    /// 
    /// Monetary
    /// Required
    /// </summary>
    [XmlElement(ElementName = "TotalDebit")]
    [Required]
    public required decimal TotalDebit { get; set; }

    /// <summary>
    /// The field shall contain the control sum of field 4.1.4.19.14. – CreditAmount, excluding the documents which content in field 4.1.4.3.1. - InvoiceStatus is "A" or "F".
    /// 
    /// Monetary
    /// Required
    /// </summary>
    [XmlElement(ElementName = "TotalCredit")]
    [Required]
    public required decimal TotalCredit { get; set; }

    /// <summary>
    /// TODO: Add documentation
    /// </summary>
    [XmlElement(ElementName = "Payment")]
    public List<PaymentDocument>? Payment { get; set; }
}

