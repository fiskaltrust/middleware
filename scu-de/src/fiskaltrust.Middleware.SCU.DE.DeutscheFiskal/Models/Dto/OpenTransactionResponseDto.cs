using System;
using Newtonsoft.Json;

namespace fiskaltrust.Middleware.SCU.DE.DeutscheFiskal.Models
{
    public class OpenTransactionResponseDto
    {
        [JsonProperty("transactionNumber")]
        public int TransactionNumber { get; set; }

        [JsonProperty("externalTransactionId")]
        public Guid ExternalTransactionId { get; set; }

        [JsonProperty("clientId")]
        public string ClientId { get; set; }

        [JsonProperty("state")]
        public string state { get; set; }

        [JsonProperty("logTime")]
        public DateTime? LogTime { get; set; }
    }
}
