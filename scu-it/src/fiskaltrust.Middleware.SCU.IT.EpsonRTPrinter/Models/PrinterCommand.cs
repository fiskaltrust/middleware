using System.Xml.Serialization;

namespace fiskaltrust.Middleware.SCU.IT.EpsonRTPrinter.Models
{
    [XmlType("printerCommand")]
    public class PrinterCommand
    {
        [XmlElement(ElementName = "resetPrinter")]
        public ResetPrinter? ResetPrinter { get; set; }

        [XmlElement(ElementName = "directIO")]
        public DirectIO? DirectIO { get; set; }
    }

    [XmlType("resetPrinter")]
    public class ResetPrinter
    {
        [XmlAttribute(AttributeName = "operator")]
        public string? Operator { get; set; }
    }

    [XmlType("directIO")]
    public class DirectIO
    {
        [XmlAttribute(AttributeName = "command")]
        public string? Command { get; set; }

        [XmlAttribute(AttributeName = "data")]
        public string? Data { get; set; }

        public static DirectIO GetSerialNrCommand() => new() { Command = "3217", Data = "00" };
    }

    [XmlType("response")]
    public class PrinterCommandResponse
    {
        [XmlAttribute(AttributeName = "success")]
        public bool Success { get; set; }

        [XmlAttribute(AttributeName = "code")]
        public string? Code { get; set; }

        [XmlAttribute(AttributeName = "status")]
        public string? Status { get; set; }

        [XmlElement(ElementName = "addInfo")]
        public CommandResponse? CommandResponse { get; set; }
    }

    [XmlType("addInfo")]
    public class CommandResponse
    {
        [XmlElement(ElementName = "printerStatus")]
        public string? PrinterStatus { get; set; }

        [XmlElement(ElementName = "responseData")]
        public string? ResponseData { get; set; }
    }
}
