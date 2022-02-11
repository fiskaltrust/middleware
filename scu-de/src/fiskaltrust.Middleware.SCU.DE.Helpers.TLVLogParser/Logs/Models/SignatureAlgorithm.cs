using System.Collections.Generic;

namespace fiskaltrust.Middleware.SCU.DE.Helpers.TLVLogParser.Logs.Models
{
    public class SignatureAlgorithm
    {
        public string Oid { get; set; }
        public string Algorithm { get; set; }
        public List<string> Parameters { get; set; }

        public static string NameFromOid(string oidName)
        {
            if (oidNameDictionary.ContainsKey(oidName))
            {
                return oidNameDictionary[oidName];
            }
            else
            {
                return oidName;
            }
        }

        // see also http://www.oid-info.com/
        private static readonly Dictionary<string, string> oidNameDictionary = new Dictionary<string, string>
        {
            {"0.4.0.127.0.7.1.1.4.1.2","ecdsa-plain-SHA224" },
            {"0.4.0.127.0.7.1.1.4.1.3","ecdsa-plain-SHA256" },
            {"0.4.0.127.0.7.1.1.4.1.4","ecdsa-plain-SHA384" },
            {"0.4.0.127.0.7.1.1.4.1.5","ecdsa-plain-SHA512" },
            {"0.4.0.127.0.7.1.1.4.1.8","ecdsa-plain-SHA3-224" },
            {"0.4.0.127.0.7.1.1.4.1.9","ecdsa-plain-SHA3-256" },
            {"0.4.0.127.0.7.1.1.4.1.10","ecdsa-plain-SHA3-384" },
            {"0.4.0.127.0.7.1.1.4.1.11","ecdsa-plain-SHA3-512" },
            {"0.4.0.127.0.7.1.1.4.4.1","ecsdsa-plain-SHA224" },
            {"0.4.0.127.0.7.1.1.4.4.2","ecsdsa-plain-SHA256" },
            {"0.4.0.127.0.7.1.1.4.4.3","ecsdsa-plain-SHA384" },
            {"0.4.0.127.0.7.1.1.4.4.4","ecsdsa-plain-SHA512" },
            {"0.4.0.127.0.7.1.1.4.4.5","ecsdsa-plain-SHA3-224" },
            {"0.4.0.127.0.7.1.1.4.4.6","ecsdsa-plain-SHA3-256" },
            {"0.4.0.127.0.7.1.1.4.4.7","ecsdsa-plain-SHA3-384" },
            {"0.4.0.127.0.7.1.1.4.4.8","ecsdsa-plain-SHA3-512" }
        };
    }
}
