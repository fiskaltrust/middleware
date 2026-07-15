using System.Xml.Serialization;

namespace fiskaltrust.Middleware.SCU.IT.CustomRTPrinter.Models.Requests
{
    [XmlRoot("printRecSubtotalAdjustment")]
    public class PrintRecSubtotalAdjustment : IFiscalRecord
    {
        [XmlAttribute("adjustmentType")]
        public uint AdjustmentType { get; set; }

        [XmlAttribute("description")]
        public string Description { get; set; }

        [XmlAttribute("amount")]
        public decimal Amount { get; set; }
    }
}
