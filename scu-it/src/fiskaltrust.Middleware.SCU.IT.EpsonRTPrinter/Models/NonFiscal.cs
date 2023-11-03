using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;

namespace fiskaltrust.Middleware.SCU.IT.EpsonRTPrinter.Models
{
    [XmlType("beginNonFiscal")]
    public class BeginNonFiscal
    {
        [XmlAttribute(AttributeName = "operator")]
        public string? Operator { get; set; }
    }

    [XmlType("endNonFiscal")]
    public class EndNonFiscal
    {
        [XmlAttribute(AttributeName = "operator")]
        public string? Operator { get; set; }
    }

    [XmlType("printNormal")]
    public class PrintNormal
    {
        [XmlAttribute(AttributeName = "operator")]
        public string? Operator { get; set; }

        [XmlAttribute(AttributeName = "font")]
        public int Font { get; set; }

        [XmlAttribute(AttributeName = "data")]
        public string? Data { get; set; }
    }

    [XmlType("printerNonFiscal")]
    public class PrinterNonFiscal
    {
        [XmlElement(ElementName = "beginNonFiscal")]
        public BeginNonFiscal BeginNonFiscal { get; set; } = new BeginNonFiscal();

        [XmlElement(ElementName = "printNormal")]
        public List<PrintNormal> PrintNormals { get; set; } = new List<PrintNormal>();


        [XmlElement(ElementName = "endNonFiscal")]
        public EndNonFiscal EndNonFiscal { get; set; } = new EndNonFiscal();

    }
}
