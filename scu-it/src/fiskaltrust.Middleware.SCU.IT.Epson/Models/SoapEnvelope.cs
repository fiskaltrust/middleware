using System;
using System.IO;
using System.Reflection;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace fiskaltrust.Middleware.SCU.IT.Epson.Models
{
    [XmlRoot(ElementName = "Envelope", Namespace = "http://schemas.xmlsoap.org/soap/envelope/")]
    public class SoapEnvelope<T> where T : class
    {
        [XmlElement("Body", Namespace = "http://schemas.xmlsoap.org/soap/envelope/")]
        public SoapBody<T>? Body { get; set; }
    }

    public class SoapBody<T> : IXmlSerializable where T : class
    {
        public T? Value { get; set; }

        public XmlSchema? GetSchema() => null;

        public void ReadXml(XmlReader reader)
        {
            var innerTypeName = typeof(T).GetCustomAttribute<XmlTypeAttribute>()?.TypeName;
            if(innerTypeName == null)
            {
                throw new InvalidOperationException("Could not deserialize the device's response, because the given type does not have the XmlType attribute specified.");
            }
            
            reader.ReadToDescendant(innerTypeName);
            var serializer = new XmlSerializer(typeof(T));
            Value = serializer.Deserialize(reader) as T;
        }

        public void WriteXml(XmlWriter writer)
        {
            var ns = new XmlSerializerNamespaces();
            ns.Add("", "");

            var innerSerializer = new XmlSerializer(typeof(T));
            innerSerializer.Serialize(writer, Value, ns);
        }
    }
}
