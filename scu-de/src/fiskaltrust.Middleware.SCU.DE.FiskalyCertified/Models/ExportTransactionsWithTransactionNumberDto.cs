using System;
using Newtonsoft.Json;

namespace fiskaltrust.Middleware.SCU.DE.FiskalyCertified.Models
{
    public class ExportTransactionsWithTransactionNumberDto
    {
        [JsonProperty("client_id")]
        public Guid ClientId { get; set; }

        [JsonProperty("start_transaction_number")]
        public string StartTransactionNumber { get; set; }

        [JsonProperty("end_transaction_number")]
        public string EndTransactionNumber { get; set; }
    }
}
