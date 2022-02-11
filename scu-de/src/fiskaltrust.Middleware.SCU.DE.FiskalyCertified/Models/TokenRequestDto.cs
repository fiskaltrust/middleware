using Newtonsoft.Json;

namespace fiskaltrust.Middleware.SCU.DE.FiskalyCertified.Models
{
    public class TokenRequestDto
    {
        [JsonProperty("api_key")]
        public string ApiKey { get; set; }
        
        [JsonProperty("api_secret")]
        public string ApiSecret { get; set; }
    }
}
