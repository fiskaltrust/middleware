using System;
using System.Diagnostics;
using System.Linq;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;
using Newtonsoft.Json;

namespace fiskaltrust.Middleware.SCU.DE.Epson
{
    public static class EpsonUtilities
    {
        public static string EscapeJsonString<T>(T value) =>
            JsonConvert.SerializeObject(value)
                .Replace("null", "\"\"")
                .Replace("\"", "&quot;")
                .Replace(Environment.NewLine, string.Empty);

        public static string GetXmlFieldValue(string xmlString, string fieldName) => XDocument.Parse(xmlString.Replace("\x00", "")).Descendants(fieldName).Select(n => n.Value).FirstOrDefault();

        public static string GenerateHash(string challenge, string sharedSecret)
        {
            using (var hasher = SHA256.Create())
            {
                var hashValue = hasher.ComputeHash(Encoding.UTF8.GetBytes($"{challenge}{sharedSecret}"));
                return Convert.ToBase64String(hashValue);
            }
        }
    }
}