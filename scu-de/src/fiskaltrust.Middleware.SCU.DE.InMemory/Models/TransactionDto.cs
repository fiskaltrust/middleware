using System;
using Newtonsoft.Json;

namespace fiskaltrust.Middleware.SCU.DE.InMemory.Models
{
    public class TransactionDto
    {
        [JsonProperty("number")]
        public uint Number { get; set; }

        [JsonProperty("time_start")]
        public DateTime TimeStart { get; set; }

        [JsonProperty("client_serial_number")]
        public string ClientSerialNumber { get; set; }

        [JsonProperty("certificate_serial")]
        public string CertificateSerial { get; set; }

        [JsonProperty("schema")]
        public TransactionDataDto Schema { get; set; }

        [JsonProperty("latest_revision")]
        public int LatestRevision { get; set; }

        [JsonProperty("log")]
        public TransactionLogDto Log { get; set; }

        [JsonProperty("signature")]
        public TransactionSignatureDto Signature { get; set; }

        [JsonProperty("vorgangsart")]
        public string Vorgangsart { get; set; }

    }
}
