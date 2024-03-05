using System.Collections.Generic;
using Newtonsoft.Json;

namespace fiskaltrust.Middleware.SCU.DE.FiskalyCertified.Models
{
    public class TssDto : FiskalyApiDto
    {
        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("state")]
        public string State { get; set; }

        [JsonProperty("metadata")]
        public Dictionary<string, object> Metadata { get; set; }

        [JsonProperty("time_creation")]
        public long TimeCreation { get; set; }

        [JsonProperty("time_init")]
        public long TimeInit { get; set; }

        [JsonProperty("time_disable")]
        public long TimeDisable { get; set; }

        [JsonProperty("certificate")]
        public string Certificate { get; set; }

        [JsonProperty("public_key")]
        public string PublicKey { get; set; }

        [JsonProperty("signature_counter")]
        public long SignatureCounter { get; set; }

        [JsonProperty("signature_algorithm")]
        public string SignatureAlgorithm { get; set; }

        [JsonProperty("signature_timestamp_format")]
        public string SignatureTimestampFormat { get; set; }

        [JsonProperty("transaction_counter")]
        public long TransactionCounter { get; set; }

        [JsonProperty("transaction_data_encoding")]
        public string TransactionDataEncoding { get; set; }

        [JsonProperty("serial_number")]
        public string SerialNumber { get; set; }

        [JsonProperty("max_number_active_transactions")]
        public int? MaxNumberOfActiveTransactions { get; set; }

        [JsonProperty("max_number_registered_clients")]
        public int? MaxNumberOfRegisteredClients { get; set; }

        [JsonProperty("number_active_transactions")]
        public int? NumberOfActiveTransactions { get; set; }
    }

    public class TssCreationDto : TssDto
    {
        [JsonProperty("admin_puk")]
        public string AdminPuk { get; set; }
    }
}
