using System.IO;
using System.Text;
using System.Xml.Serialization;
using fiskaltrust.Middleware.SCU.IT.Epson.Models;

namespace fiskaltrust.Middleware.SCU.IT.Epson.Utilities
{
    public static class SoapSerializer
    {
        public static string Serialize<T>(T body) where T : class
        {
            var ns = new XmlSerializerNamespaces();
            ns.Add("soapenv", "http://schemas.xmlsoap.org/soap/envelope/");
            ns.Add("", "");

            var envelope = new SoapEnvelope<T> { Body = new() { Value = body } };

            var serializer = new XmlSerializer(typeof(SoapEnvelope<T>));
            using var textWriter = new Utf8StringWriter();
            serializer.Serialize(textWriter, envelope, ns);
            
            return textWriter.ToString();
        }

        public static T? Deserialize<T>(Stream content) where T : class
        {
            var serializer = new XmlSerializer(typeof(SoapEnvelope<T>));
            var envelope = serializer.Deserialize(content) as SoapEnvelope<T>;
            return envelope?.Body?.Value;
        }
    }

    internal class Utf8StringWriter : StringWriter
    {
        public override Encoding Encoding => Encoding.UTF8;
    }
}
