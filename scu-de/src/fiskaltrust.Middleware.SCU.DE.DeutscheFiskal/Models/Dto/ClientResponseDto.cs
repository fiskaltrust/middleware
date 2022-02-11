using Newtonsoft.Json;

namespace fiskaltrust.Middleware.SCU.DE.DeutscheFiskal.Models
{
    public class ClientResponseDto
    {
        [JsonProperty("clientId")]
        public string ClientId { get; set; }
    }
}
