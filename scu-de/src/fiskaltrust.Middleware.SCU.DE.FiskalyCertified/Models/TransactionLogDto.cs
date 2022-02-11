using Newtonsoft.Json;

namespace fiskaltrust.Middleware.SCU.DE.FiskalyCertified.Models
{
    public class TransactionLogDto
    {
        [JsonProperty("operation")]
        public string Operation { get; set; }

        [JsonProperty("timestamp")]
        public long Timestamp { get; set; }

        [JsonProperty("timestamp_format")]
        public string TimestampFormat { get; set; }
    }
}
