using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace fiskaltrust.Middleware.SCU.DE.FiskalyCertified.Models
{
    public class TransactionDto
    {
        [JsonProperty("tss_id")]
        public Guid TssId { get; set; }

        [JsonProperty("tss_serial_number")]
        public string TssSerialNumber { get; set; }

        [JsonProperty("client_id")]
        public Guid ClientId { get; set; }

        [JsonProperty("client_serial_number")]
        public string ClientSerialNumber { get; set; }

        [JsonProperty("state")]
        public string State { get; set; }

        [JsonProperty("revision")]
        public int Revision { get; set; }

        [JsonProperty("latest_revision")]
        public int LatestRevision { get; set; }

        [JsonProperty("number")]
        public uint Number { get; set; }

        [JsonProperty("time_start")]
        public long TimeStart { get; set; }

        [JsonProperty("log")]
        public TransactionLogDto Log { get; set; }

        [JsonProperty("signature")]
        public TransactionSignatureDto Signature { get; set; }

        [JsonProperty("schema")]
        public TransactionDataDto Schema { get; set; }

        [JsonProperty("time_end")]
        public long TimeEnd { get; set; }

        [JsonProperty("qr_code_data")]
        public string QrCodeData { get; set; }

        [JsonProperty("metadata")]
        public Dictionary<string, object> metadata { get; set; }
    }
}
