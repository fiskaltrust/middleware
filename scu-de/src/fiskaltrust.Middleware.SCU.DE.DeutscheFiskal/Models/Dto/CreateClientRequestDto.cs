using Newtonsoft.Json;

namespace fiskaltrust.Middleware.SCU.DE.DeutscheFiskal.Models
{
    public class CreateClientRequestDto
    {
        [JsonProperty("registrationToken")]
        public string RegistrationToken { get; set; }

        [JsonProperty("uniqueClientId")]
        public string ClientId { get; set; }

        [JsonProperty("briefDescription")]
        public string BriefDescription { get; set; }

        [JsonProperty("typeOfSystem")]
        public string TypeOfSystem { get; set; }

        [JsonProperty("ersIdentifier")]
        public string ErsIdentifier { get; set; }
    }
}
