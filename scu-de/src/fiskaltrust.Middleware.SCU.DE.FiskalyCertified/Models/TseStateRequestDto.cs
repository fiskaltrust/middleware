using Newtonsoft.Json;
using fiskaltrust.Middleware.SCU.DE.FiskalyCertified.Models;

namespace fiskaltrust.Middleware.SCU.DE.FiskalyCertified.Models
{
    public class TseStateRequestDto
    {
        [JsonProperty("state")]
        public string State { get; set; }
    }
}
