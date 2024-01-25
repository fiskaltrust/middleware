using System;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace fiskaltrust.Middleware.SCU.IT.CustomRTPrinter.Models.Requests
{
    [XmlRoot("printRecItem")]
    public class PrintRecItem : IRecord
    {
        [XmlAttribute("description")]
        public string Description { get; set; }

        [XmlAttribute("unitPrice")]
        public decimal UnitPrice { get; set; }

        [XmlAttribute("department")]
        public int Department { get; set; }

        [XmlAttribute("IdVat")]
        public string IdVat { get; set; }

        [XmlAttribute("quantity")]
        public decimal Quantity { get; set; }
    }
}