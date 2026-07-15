using System.Xml.Serialization;

namespace fiskaltrust.Middleware.SCU.IT.CustomRTPrinter.Models.Requests
{
    [XmlRoot("displayText")]
    public class DisplayText : IFiscalRecord
    {
        [XmlAttribute("data")]
        public string Data { get; set; }
    }
}
