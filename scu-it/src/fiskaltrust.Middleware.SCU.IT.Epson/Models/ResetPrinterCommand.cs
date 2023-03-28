using System.Xml.Serialization;

namespace fiskaltrust.Middleware.SCU.IT.Epson.Models
{
    [XmlType("printerCommand")]
    public class ResetPrinterCommand
    {
        [XmlElement(ElementName = "resetPrinter")]
        public ResetPrinter? ResetPrinter { get; set; }
    }

    [XmlType("resetPrinter")]
    public class ResetPrinter
    {
        [XmlAttribute(AttributeName = "operator")]
        public string? Operator { get; set; }
    }
}
