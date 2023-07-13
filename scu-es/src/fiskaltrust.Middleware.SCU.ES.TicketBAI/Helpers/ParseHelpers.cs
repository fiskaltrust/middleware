using System.IO;
using System.Xml;
using System.Xml.Serialization;

namespace fiskaltrust.Middleware.SCU.ES.TicketBAI.Helpers
{
    public static class ParseHelpers
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
    }
}
