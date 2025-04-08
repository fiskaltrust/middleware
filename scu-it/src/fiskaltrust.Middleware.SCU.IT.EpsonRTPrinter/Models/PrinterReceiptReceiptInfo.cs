using System.Xml.Serialization;

namespace fiskaltrust.Middleware.SCU.IT.EpsonRTPrinter.Models;

[XmlType("addInfo")]
public class PrinterReceiptReceiptInfo
{
    [XmlElement(ElementName = "lastCommand")]
    public string? LastCommand { get; set; }

    [XmlElement(ElementName = "printerStatus")]
    public string? PrinterStatus { get; set; }

    [XmlElement(ElementName = "fiscalReceiptNumber")]
    public string? FiscalReceiptNumber { get; set; }

    [XmlElement(ElementName = "fiscalReceiptAmount")]
    public string? FiscalReceiptAmount { get; set; }

    [XmlElement(ElementName = "fiscalReceiptDate")]
    public string? FiscalReceiptDate { get; set; }

    [XmlElement(ElementName = "fiscalReceiptTime")]
    public string? FiscalReceiptTime { get; set; }

    [XmlElement(ElementName = "receiptISODateTime")]
    public string? ReceiptISODateTime { get; set; }

    [XmlElement(ElementName = "zRepNumber")]
    public string? ZRepNumber { get; set; }

    [XmlElement(ElementName = "serialNumber")]
    public string? SerialNumber { get; set; }
}