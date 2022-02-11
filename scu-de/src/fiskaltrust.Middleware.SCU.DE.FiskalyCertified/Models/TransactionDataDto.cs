using Newtonsoft.Json;

namespace fiskaltrust.Middleware.SCU.DE.FiskalyCertified.Models
{
    public class TransactionDataDto
    {
        [JsonProperty("raw")]
        public RawData RawData { get; set; }
    }
}
