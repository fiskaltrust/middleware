using System.Xml.Serialization;

namespace fiskaltrust.Middleware.SCU.IT.CustomRTPrinter.Models.Requests
{
    [XmlRoot("printRecMessage")]
    public class PrintRecMessage : IFiscalRecord
    {
        [XmlAttribute("message")]
        public string Message { get; set; }

        [XmlAttribute("messageType")]
        public uint MessageType { get; set; }

        [XmlAttribute("font")]
        public uint Font { get; set; }
    }
}
