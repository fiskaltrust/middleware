using System.Xml;
using System.Xml.Serialization;

namespace fiskaltrust.Middleware.SCU.IT.CustomRTPrinter.Models.Requests
{
    [XmlRoot("printRecItemVoid")]
    public class PrintRecItemVoid : IFiscalRecord
    {
        [XmlAttribute("description")]
        public string Description { get; set; }

        [XmlAttribute("unitPrice")]
        public decimal UnitPrice { get; set; }

        [XmlAttribute("department")]
        public uint Department { get; set; }

        [XmlAttribute("idVat")]
        public uint IdVat { get; set; }

        [XmlAttribute("quantity")]
        public decimal Quantity { get; set; }
    }
}