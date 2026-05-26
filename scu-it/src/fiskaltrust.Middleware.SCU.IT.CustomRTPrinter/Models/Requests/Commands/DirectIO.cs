using System.Xml.Serialization;

namespace fiskaltrust.Middleware.SCU.IT.CustomRTPrinter.Models.Requests
{
    [XmlRoot("directIO")]
    public class DirectIO : ICommand
    {
        [XmlAttribute("command")]
        public string Command { get; set; }

        [XmlAttribute("data")]
        public string Data { get; set; } = "";
    }
}
