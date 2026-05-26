using System.Xml.Serialization;

namespace fiskaltrust.Middleware.SCU.IT.CustomRTPrinter.Models.Requests
{
    [XmlRoot("printRecRefund")]
    public class PrintRecRefund : IFiscalRecord
    {
        [XmlAttribute("description")]
        public string Description { get; set; }

        [XmlAttribute("quantity")]
        public decimal Quantity { get; set; }

        [XmlAttribute("unitPrice")]
        public decimal UnitPrice { get; set; }

        [XmlAttribute("idVat")]
        public uint IdVat { get; set; }
    }
}