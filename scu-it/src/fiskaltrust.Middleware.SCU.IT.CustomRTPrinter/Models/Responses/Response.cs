using System.Xml.Serialization;

namespace fiskaltrust.Middleware.SCU.IT.CustomRTPrinter.Models.Responses
{
    public interface IResponse { }

    [XmlRoot("response")]
    public class Response<T> : IResponse
    {
        [XmlAttribute("success")]
        public bool Success { get; set; }

        [XmlAttribute("status")]
        public int Status { get; set; }


        [XmlElement("addInfo")]
        public AddInfo<T> AddInfo { get; set; }
    }
}