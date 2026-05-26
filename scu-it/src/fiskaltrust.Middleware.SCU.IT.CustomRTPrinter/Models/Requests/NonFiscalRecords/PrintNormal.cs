using System.Xml.Serialization;

namespace fiskaltrust.Middleware.SCU.IT.CustomRTPrinter.Models.Requests
{
    [XmlRoot("printNormal")]
    public class PrintNormal : INonFiscalRecord
    {
        [XmlAttribute("font")]
        public int Font { get; set; } = 1;

        [XmlAttribute("data")]
        public string? Data { get; set; }
    }
}
