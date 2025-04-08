using System.Xml.Serialization;

namespace fiskaltrust.Middleware.SCU.IT.EpsonRTPrinter.Models;

[XmlType("response")]
public class PrinterReceiptResponse
{
    [XmlAttribute(AttributeName = "success")]
    public bool Success { get; set; }

    [XmlAttribute(AttributeName = "code")]
    public string? Code { get; set; }

    [XmlAttribute(AttributeName = "status")]
    public string? Status { get; set; }

    [XmlElement(ElementName = "addInfo")]
    public PrinterReceiptReceiptInfo? Receipt { get; set; }
}
