using System.Xml.Serialization;

namespace fiskaltrust.Middleware.SCU.IT.EpsonRTPrinter.Models
{
    [XmlType("response")]
    public class ReportResponse
    {
        [XmlAttribute(AttributeName = "success")]
        public bool Success { get; set; }

        [XmlAttribute(AttributeName = "code")]
        public string? Code { get; set; }

        [XmlAttribute(AttributeName = "status")]
        public string? Status { get; set; }

        [XmlElement(ElementName = "addInfo")]
        public ReportInfo? ReportInfo { get; set; }
    }

    [XmlType("addInfo")]
    public class ReportInfo
    {
        [XmlElement(ElementName = "printerStatus")]
        public string? PrinterStatus { get; set; }

        [XmlElement(ElementName = "zRepNumber")]
        public string? ZRepNumber { get; set; }

        [XmlElement(ElementName = "dailyAmount")]
        public string? DailyAmount { get; set; }

    }
}
