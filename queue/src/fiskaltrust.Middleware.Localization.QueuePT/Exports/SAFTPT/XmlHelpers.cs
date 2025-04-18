using System.IO;
using System.Xml.Serialization;
using fiskaltrust.SAFT.CLI.SAFTSchemaPT10401;

namespace fiskaltrust.Middleware.Localization.QueuePT.Exports.SAFTPT;

public static class XmlHelpers
{
    public static void SerializeAuditFile(AuditFile auditFile, string path)
    {
        var serializer = new XmlSerializer(typeof(AuditFile));
        using var reader = File.CreateText(path);
        serializer.Serialize(reader, auditFile);
    }
}