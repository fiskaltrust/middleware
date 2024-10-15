using System.Xml.Serialization;

namespace fiskaltrust.Middleware.SCU.IT.CustomRTPrinter.Models.Requests
{
    [XmlRoot("setLotteryCode")]
    public class SetLotteryCode : IFiscalRecord
    {
        [XmlAttribute("code")]
        public string Code { get; set; }
    }
}