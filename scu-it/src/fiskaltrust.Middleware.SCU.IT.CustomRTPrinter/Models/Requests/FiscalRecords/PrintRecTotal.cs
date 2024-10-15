using System.Xml;
using System.Xml.Serialization;

namespace fiskaltrust.Middleware.SCU.IT.CustomRTPrinter.Models.Requests
{
    [XmlRoot("printRecTotal")]
    public class PrintRecTotal : IFiscalRecord
    {
        [XmlAttribute("description")]
        public string Description { get; set; }

        [XmlAttribute("payment")]
        public decimal Payment { get; set; }

        [XmlAttribute("paymentType")]
        public uint PaymentType { get; set; }

        [XmlAttribute("paymentQty")]
        public uint PaymentQty { get; set; }
    }
}