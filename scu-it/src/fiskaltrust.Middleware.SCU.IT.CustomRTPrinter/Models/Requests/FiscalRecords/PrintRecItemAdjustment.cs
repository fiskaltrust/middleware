using System.Xml.Serialization;

namespace fiskaltrust.Middleware.SCU.IT.CustomRTPrinter.Models.Requests
{
    [XmlRoot("printRecItemAdjustment")]
    public class PrintRecItemAdjustment : IFiscalRecord
    {
        // 1=percent discount, 2=percent surcharge, 3=amount discount, 4=amount surcharge (applied to last item).
        [XmlAttribute("adjustmentType")]
        public uint AdjustmentType { get; set; }

        [XmlAttribute("description")]
        public string Description { get; set; }

        [XmlAttribute("amount")]
        public decimal Amount { get; set; }

        [XmlAttribute("department")]
        public uint Department { get; set; } = 1;

        [XmlAttribute("idVat")]
        public uint IdVat { get; set; }

        public bool ShouldSerializeIdVat() => IdVat != 0;
    }
}
