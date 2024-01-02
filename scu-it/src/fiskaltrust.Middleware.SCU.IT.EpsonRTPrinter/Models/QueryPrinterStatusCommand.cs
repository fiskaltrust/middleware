using System.Xml.Serialization;

namespace fiskaltrust.Middleware.SCU.IT.EpsonRTPrinter.Models
{
    [XmlType("printerCommand")]
    public class QueryPrinterStatusCommand
    {
        [XmlElement(ElementName = "queryPrinterStatus")]
        public QueryPrinterStatus? QueryPrinterStatus { get; set; }
    }

    [XmlType("queryPrinterStatus")]
    public class QueryPrinterStatus
    {
        [XmlAttribute(AttributeName = "operator")]
        public string? Operator { get; set; }

        [XmlAttribute(AttributeName = "statusType")]
        public int StatusType { get; set; } = 0;
    }
}
