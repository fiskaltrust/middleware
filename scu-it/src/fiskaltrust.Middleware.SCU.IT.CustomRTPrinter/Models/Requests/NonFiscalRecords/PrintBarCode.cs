using System.Xml.Serialization;

namespace fiskaltrust.Middleware.SCU.IT.CustomRTPrinter.Models.Requests
{
    [XmlRoot("printBarCode")]
    public class PrintBarCode : INonFiscalRecord
    {
        [XmlAttribute("hRIPosition")]
        public int HriPosition { get; set; } = 2;

        [XmlAttribute("codeType")]
        public int CodeType { get; set; } = 1;

        [XmlAttribute("code")]
        public string? Code { get; set; }
    }
}
