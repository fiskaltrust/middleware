using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace fiskaltrust.Middleware.SCU.IT.CustomRTPrinter.Models.Requests
{
    public interface IRecord { }

    public class Records<T> : IXmlSerializable where T : IRecord
    {
        private readonly T[] _records;
        internal Records()
        {
        }

        public Records(T[] records)
        {
            _records = records;
        }

        public XmlSchema? GetSchema() => null;
        public void ReadXml(XmlReader reader) => throw new System.NotImplementedException();
        public void WriteXml(XmlWriter writer)
        {
            var namespaces = new XmlSerializerNamespaces();
            namespaces.Add("", "");

            foreach (var record in _records)
            {
                new XmlSerializer(record.GetType()).Serialize(writer, record, namespaces);
            }
        }
    }
}