using System.Xml.Serialization;

namespace fiskaltrust.Middleware.SCU.IT.CustomRTPrinter.Models.Requests
{
    [XmlRoot("printXZReport")]
    public class PrintXZReport : IReport
    {
        [XmlAttribute("description")]
        public string Description { get; set; }
    }
}