using System.Xml.Serialization;

namespace fiskaltrust.Middleware.SCU.IT.CustomRTPrinter.Models.Requests
{
    [XmlRoot("printRecItem")]
    public class PrintRecItem : IFiscalRecord
    {
        [XmlAttribute("description")]
        public string Description { get; set; }

        [XmlAttribute("quantity")]
        public decimal Quantity { get; set; }

        public bool ShouldSerializeQuantity() => Quantity != 0;

        [XmlAttribute("unitPrice")]
        public decimal UnitPrice { get; set; }

        [XmlAttribute("department")]
        public uint Department { get; set; } = 1;

        [XmlAttribute("idVat")]
        public uint IdVat { get; set; }
    }
}