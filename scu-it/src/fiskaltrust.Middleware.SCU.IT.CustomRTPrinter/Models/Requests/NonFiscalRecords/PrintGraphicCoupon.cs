using System.Xml.Serialization;

namespace fiskaltrust.Middleware.SCU.IT.CustomRTPrinter.Models.Requests
{
    [XmlRoot("printGraphicCoupon")]
    public class PrintGraphicCoupon : INonFiscalRecord
    {
        [XmlAttribute("data")]
        public string? Data { get; set; }
    }
}
