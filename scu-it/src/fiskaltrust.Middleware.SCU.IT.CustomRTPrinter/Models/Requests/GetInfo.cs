using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace fiskaltrust.Middleware.SCU.IT.CustomRTPrinter.Models.Requests
{
    public class GetInfo : IRequest, IXmlSerializable
    {
        public XmlSchema? GetSchema() => null;
        public void ReadXml(XmlReader reader) => throw new System.NotImplementedException();
        public void WriteXml(XmlWriter writer)
        {
        }
    }
}