using System;
using System.IO;
using System.Text.Json;
using System.Xml.Serialization;
using fiskaltrust.Middleware.Localization.QueuePT.Exports.SAFTPT;
using fiskaltrust.SAFT.CLI.SAFTSchemaPT10401;
using fiskaltrust.storage.V0;

namespace fiskaltrust.Middleware.Localization.QueuePT.UnitTest.QueuePT;


public class SAFTTests
{
    public void BasicTest()
    {


    }
    private static AuditFile? GetAuditFileFromXML(string xml)
    {
        var serializer = new XmlSerializer(typeof(AuditFile));
        using var reader = new StringReader(xml);
        return (AuditFile?) serializer.Deserialize(reader);
    }
}
