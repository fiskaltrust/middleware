using System;
using Newtonsoft.Json;

namespace fiskaltrust.Middleware.SCU.DE.DeutscheFiskal.Models
{
    public class StartTransactionResponseDto
    {
        [JsonProperty("signatureCounter")]
        public ulong SignatureCounter { get; set; }

        [JsonProperty("signatureValue")]
        public string SignatureValue { get; set; }

        [JsonProperty("logTime")]
        public DateTime LogTime { get; set; }

        [JsonProperty("transactionNumber")]
        public ulong TransactionNumber { get; set; }

        [JsonProperty("serialNumber")]
        public string SerialNumber { get; set; }
    }
}
