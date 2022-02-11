using Newtonsoft.Json;

namespace fiskaltrust.Middleware.SCU.DE.InMemory.Models
{
    public class TransactionSignatureDto
    {
        [JsonProperty("value")]
        public string Value { get; set; }

        [JsonProperty("algorithm")]
        public string Algorithm { get; set; }

        [JsonProperty("counter")]
        public uint SignatureCounter { get; set; }

        [JsonProperty("public_key")]
        public string PublicKey { get; set; }
    }
}
