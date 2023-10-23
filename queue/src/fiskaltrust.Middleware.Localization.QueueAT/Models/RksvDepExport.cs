using System.Collections.Generic;
using Newtonsoft.Json;

namespace fiskaltrust.Middleware.Localization.QueueAT.Models
{
    public class RksvDepExport
    {
        [JsonProperty("Belege-Gruppe")]
        public List<RksvDepReceiptGroup> ReceiptGroups { get; set; }
    }

    public class RksvDepReceiptGroup
    {
        [JsonProperty("Kassen-ID")]
        public string CashboxIdentification { get; set; }

        [JsonProperty("Signaturzertifikat")]
        public string Certificate { get; set; }

        [JsonProperty("Zertifizierungsstellen")]
        public string[] CertificateAuthorities { get; set; }

        [JsonProperty("Belege-kompakt")]
        public IEnumerable<string> Receipts { get; set; }
    }
}
