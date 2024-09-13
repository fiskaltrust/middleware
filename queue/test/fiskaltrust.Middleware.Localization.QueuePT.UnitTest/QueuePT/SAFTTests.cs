using System;
using System.IO;
using System.Text.Json;
using System.Xml.Serialization;
using fiskaltrust.Middleware.Localization.QueuePT.Exports.SAFTPT;
using fiskaltrust.Middleware.Localization.QueuePT.Exports.SAFTPT.SAFTSchemaPT10401;

namespace fiskaltrust.Middleware.Localization.QueuePT.UnitTest.QueuePT
{
    public class SAFTTests
    {
        public void BasicTest()
        {
            var auditFile = SAFTMapping.CreateAuditFile([ReceiptExamples.NUNO_BASIC_RECEIPT]);
            XmlHelpers.SerializeAuditFile(auditFile, @"C:\GitHub\market-pt-services\SAFT\docs\examples\SAFT_Nuno_Invoice_2.xml");
            var content = File.ReadAllText(@"C:\GitHub\market-pt-services\SAFT\docs\examples\SAFT_Nuno_Invoice.xml");
            var result = GetAuditFileFromXML(content);
            Console.WriteLine(JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true }));



        }
        private static AuditFile GetAuditFileFromXML(string xml)
        {
            var serializer = new XmlSerializer(typeof(AuditFile));
            using var reader = new StringReader(xml);
            return (AuditFile) serializer.Deserialize(reader);
        }
    }
}
