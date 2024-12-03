using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using FirmaXadesNetCore.Crypto;
using FirmaXadesNetCore.Signature.Parameters;
using FirmaXadesNetCore;
using System.Security.Cryptography.X509Certificates;
using System.Net.NetworkInformation;

namespace fiskaltrust.Middleware.SCU.ES.Helpers
{
    public class Utf8StringWriter : StringWriter
    {
        public override Encoding Encoding => Encoding.UTF8;
    }

    public static class XmlHelpers
    {
        public static Stream ToStream(this string content)
        {
            var stream = new MemoryStream();
            var writer = new StreamWriter(stream, Encoding.UTF8);
            writer.Write(content);
            writer.Flush();
            stream.Position = 0;
            return stream;
        }

        public static T? Deserialize<T>(string from)
        {
            var serializer = new XmlSerializer(typeof(T));

            return (T?) serializer.Deserialize(from.ToStream());
        }

        public static string Serialize<T>(T from)
        {
            var serializer = new XmlSerializer(typeof(T));
            using var writer = new Utf8StringWriter();

            serializer.Serialize(writer, from);

            return writer.ToString();
        }

        public static string GetXMLIncludingNamespace<T>(T request, string prefix, string namespaceUri)
        {
            var doc = new XmlDocument();
            var nav = doc.CreateNavigator();
            using (var w = nav!.AppendChild())
            {
                var namespaces = new XmlSerializerNamespaces();
                namespaces.Add(prefix, namespaceUri);
                var ser = new XmlSerializer(typeof(T));
                ser.Serialize(w, request, namespaces);
            }
            return doc.OuterXml;
        }

        public static string SignXmlContentWithXades(string xml, X509Certificate2 certificate)
        {
            var xadesService = new XadesService();
            var parameters = new SignatureParameters
            {
                SignaturePackaging = SignaturePackaging.ENVELOPED,
                DataFormat = new DataFormat
                {
                    MimeType = "text/xml"
                },
                DigestMethod = DigestMethod.SHA256,
                Signer = new Signer(certificate)
            };

            var byteArray = Encoding.ASCII.GetBytes(xml);
            var stream = new MemoryStream(byteArray);
            var signedXmlBytes = xadesService.Sign(stream, parameters).GetDocumentBytes();
            return Encoding.UTF8.GetString(signedXmlBytes, 0, signedXmlBytes.Length);
        }
    }
}
