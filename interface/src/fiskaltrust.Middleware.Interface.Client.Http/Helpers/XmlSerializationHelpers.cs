using System;
using System.IO;
using System.Xml.Serialization;

namespace fiskaltrust.Middleware.Interface.Client.Http.Helpers
{
    internal static class XmlSerializationHelpers
    {
        public static string Serialize(object inputObject)
        {
            using (var writer = new StringWriter())
            {
                new XmlSerializer(inputObject.GetType()).Serialize(writer, inputObject);
                return writer.ToString();
            }
        }
    }
}
