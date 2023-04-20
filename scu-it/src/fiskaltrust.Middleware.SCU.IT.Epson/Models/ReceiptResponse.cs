using System.Xml.Serialization;

namespace fiskaltrust.Middleware.SCU.IT.Epson.Models
{
    [XmlType("response")]
    public class ReceiptResponse
    {
        [XmlAttribute(AttributeName = "success")]
        public bool Success { get; set; }

        [XmlAttribute(AttributeName = "code")]
        public string? Code { get; set; }

        [XmlAttribute(AttributeName = "status")]
        public string? Status { get; set; }

        [XmlElement(ElementName = "addInfo")]
        public ReceiptInfo? Receipt { get; set; }
    }

    [XmlType("addInfo")]
    public class ReceiptInfo
    {
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

        [XmlElement(ElementName = "zRepNumber")]
        public string? ZRepNumber { get; set; }
    }
}
