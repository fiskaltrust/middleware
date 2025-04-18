using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using FirmaXadesNetCore.Crypto;
using FirmaXadesNetCore.Signature.Parameters;
using FirmaXadesNetCore;
using System.Security.Cryptography.X509Certificates;

namespace fiskaltrust.Middleware.SCU.ES.TicketBAI.Helpers
{
    public static class XmlHelpers
    {
        public static Stream ToStream(this string content)
        {
            var stream = new MemoryStream();
            var writer = new StreamWriter(stream);
            writer.Write(content);
            writer.Flush();
            stream.Position = 0;
            return stream;
        }

        public static T? ParseXML<T>(this string content) where T : class
        {
            var reader = XmlReader.Create(content.Trim().ToStream(), new XmlReaderSettings() { ConformanceLevel = ConformanceLevel.Document });
            return new XmlSerializer(typeof(T)).Deserialize(reader) as T;
        }
        public static string GetXMLIncludingNamespace<T>(T request)
        {
            var namespaceUri = "urn:ticketbai:emision";
            var prefix = "t";
            var settings = new XmlWriterSettings
            {
                OmitXmlDeclaration = true,
                ConformanceLevel = ConformanceLevel.Document,
                Encoding = Encoding.UTF8
            };

            var stringBuilder = new StringBuilder();
            using (var writer = XmlWriter.Create(stringBuilder, settings))
            {
                var namespaces = new XmlSerializerNamespaces();
                namespaces.Add(prefix, namespaceUri);
                var serializer = new XmlSerializer(typeof(T));
                serializer.Serialize(writer, request, namespaces);
            }

            return stringBuilder.ToString();
        }

        public static string SignXmlContentWithXades(string xml, string policyIdentifier, string policyDigest, X509Certificate2 certificate)
        {
            var xadesService = new XadesService();
            var parameters = new SignatureParameters
            {
                SignaturePolicyInfo = new SignaturePolicyInfo
                {
                    PolicyIdentifier = policyIdentifier,
                    PolicyHash = policyDigest,
                    PolicyUri = policyIdentifier // The URI in our case matches the policyidentifier
                },
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
