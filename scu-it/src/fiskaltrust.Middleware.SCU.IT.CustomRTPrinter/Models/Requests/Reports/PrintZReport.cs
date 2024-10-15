using System.Xml.Serialization;

namespace fiskaltrust.Middleware.SCU.IT.CustomRTPrinter.Models.Requests
{
    [XmlRoot("printZReport")]
    public class PrintZReport : IReport
    {
        [XmlAttribute("description")]
        public string Description { get; set; }
    }
}