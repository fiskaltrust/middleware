using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;

namespace fiskaltrust.Middleware.SCU.IT.Epson.Models
{

    [XmlType("queryPrinterStatus")]
    public class QueryPrinterStatus
    {
        [XmlAttribute(AttributeName = "operator")]
        public string? Operator { get; set; }

        [XmlAttribute(AttributeName = "statusType")]
        public int StatusType { get; set; } = 0;
    }


    [XmlType("printerCommand")]
    public class PrinterCommand
    {
        [XmlElement(ElementName = "queryPrinterStatus")]
        public QueryPrinterStatus? DisplayText { get; set; }

    }
}
