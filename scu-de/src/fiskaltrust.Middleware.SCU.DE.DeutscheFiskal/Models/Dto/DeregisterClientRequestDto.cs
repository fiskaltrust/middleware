using Newtonsoft.Json;

namespace fiskaltrust.Middleware.SCU.DE.DeutscheFiskal.Models
{
    public class DeregisterClientRequestDto
    {
        [JsonProperty("uniqueClientId")]
        public string ClientId { get; set; }
    }
}
