using Newtonsoft.Json;

namespace fiskaltrust.Middleware.SCU.DE.InMemory.Models
{
    public class TransactionLogDto
    {

        [JsonProperty("timestamp")]
        public long Timestamp { get; set; }

        [JsonProperty("timestamp_format")]
        public string TimestampFormat { get; set; }
    }
}
