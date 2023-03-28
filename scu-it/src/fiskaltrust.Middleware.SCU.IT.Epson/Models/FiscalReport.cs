using System.Xml.Serialization;

namespace fiskaltrust.Middleware.SCU.IT.Epson.Models
{

    [XmlType("printerFiscalReport")]
    public class FiscalReport
    {
        [XmlElement(ElementName = "displayText")]
        public DisplayText? DisplayText { get; set; }

        [XmlElement(ElementName = "printZReport")]
        public ZReport? ZReport { get; set; }

    }

    [XmlType("printZReport")]
    public class ZReport
    {
        [XmlAttribute(AttributeName = "operator")]
        public string? Operator { get; set; }

        [XmlAttribute(AttributeName = "timeout")]
        public int? Timeout { get; set; }
    }

    [XmlType("printXReport")]
    public class XReport
    {
        [XmlAttribute(AttributeName = "operator")]
        public string? Operator { get; set; }
    }

    [XmlType("printXZReport")]
    public class XZReport
    {
        [XmlAttribute(AttributeName = "operator")]
        public string? Operator { get; set; }

        [XmlAttribute(AttributeName = "timeout")]
        public int? Timeout { get; set; }
    }
}
