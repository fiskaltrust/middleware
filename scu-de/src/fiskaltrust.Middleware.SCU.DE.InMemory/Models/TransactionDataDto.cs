using Newtonsoft.Json;

namespace fiskaltrust.Middleware.SCU.DE.InMemory.Models
{
    public class TransactionDataDto
    {
        [JsonProperty("raw")]
        public RawData RawData { get; set; }
    }
}
