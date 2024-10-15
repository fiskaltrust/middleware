using System.Xml.Serialization;

namespace fiskaltrust.Middleware.SCU.IT.EpsonRTPrinter.Models
{

    [XmlType("printerFiscalReport")]
    public class FiscalReport
    {
        [XmlElement(ElementName = "printZReport")]
        public ZReport? ZReport { get; set; }

        [XmlElement(ElementName = "printXReport")]
        public XReport? XReport { get; set; }
    }

    [XmlType("printZReport")]
    public class ZReport
    {
        [XmlAttribute(AttributeName = "operator")]
        public string? Operator { get; } = "1";

        [XmlIgnore]
        public int? Timeout { get; set; }

        [XmlAttribute(AttributeName = "timeout")]
        public string? TimeoutStr
        {
            get => Timeout.HasValue ? Timeout.ToString() : null;

            set
            {
                if (int.TryParse(value, out var timeout))
                {
                    Timeout = timeout;
                }
            }
        }
    }

    [XmlType("printXReport")]
    public class XReport
    {
        [XmlAttribute(AttributeName = "operator")]
        public string? Operator { get; } = "1";
    }

    [XmlType("printXZReport")]
    public class XZReport
    {
        [XmlAttribute(AttributeName = "operator")]
        public string? Operator { get; } = "1";

        [XmlIgnore]
        public int? Timeout { get; set; }

        [XmlAttribute(AttributeName = "timeout")]
        public string? TimeoutStr
        {
            get => Timeout.HasValue ? Timeout.ToString() : null;

            set
            {
                if (int.TryParse(value, out var timeout))
                {
                    Timeout = timeout;
                }
            }
        }
    }
}
