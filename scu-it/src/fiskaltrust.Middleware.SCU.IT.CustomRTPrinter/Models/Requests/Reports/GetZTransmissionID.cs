using System.Xml.Serialization;

namespace fiskaltrust.Middleware.SCU.IT.CustomRTPrinter.Models.Requests
{
    [XmlRoot("getZTransmissionID")]
    public class GetZTransmissionID : IReport
    {
        [XmlAttribute("zNum")]
        public uint zNum { get; set; }
    }
}