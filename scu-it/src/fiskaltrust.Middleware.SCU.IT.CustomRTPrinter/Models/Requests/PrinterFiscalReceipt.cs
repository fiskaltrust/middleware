using System.Linq;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace fiskaltrust.Middleware.SCU.IT.CustomRTPrinter.Models.Requests
{
    [XmlRoot("printerFiscalReceipt")]
    public class PrinterFiscalReceipt : IRequest
    {
        [XmlElement("beginFiscalReceipt")]
        public BeginFiscalReceipt BeginFiscalReceipt { get; set; } = new();

        [XmlAnyElement()]
        public Records Records { get; set; }

        [XmlElement("endFiscalReceipt")]
        public EndFiscalReceipt EndFiscalReceipt { get; set; } = new();
    }

    public interface IRecord { }
    public class Records : IXmlSerializable
    {
        private readonly IRecord[] _records;
        internal Records()
        {
        }

        public Records(IRecord[] records)
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

    [XmlRoot("beginFiscalReceipt")]
    public class BeginFiscalReceipt
    {
    }

    [XmlRoot("endFiscalReceipt")]
    public class EndFiscalReceipt
    {
    }
}