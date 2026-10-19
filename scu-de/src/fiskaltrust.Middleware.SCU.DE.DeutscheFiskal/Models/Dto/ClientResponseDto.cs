using System;
using Newtonsoft.Json;

namespace fiskaltrust.Middleware.SCU.DE.DeutscheFiskal.Models
{
    public class ClientResponseDto
    {
        private const string REMOVED_STATE = "removed";

        [JsonProperty("clientId")]
        public string ClientId { get; set; }

        [JsonProperty("clientState")]
        public string ClientState { get; set; }

        // Deregistered clients are kept by the FCC with state "removed", but can no longer authenticate against the ERS API
        [JsonIgnore]
        public bool IsRemoved => string.Equals(ClientState, REMOVED_STATE, StringComparison.OrdinalIgnoreCase);
    }
}
