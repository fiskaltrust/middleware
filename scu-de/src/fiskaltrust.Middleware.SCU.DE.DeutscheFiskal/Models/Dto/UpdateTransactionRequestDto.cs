using System;
using Newtonsoft.Json;

namespace fiskaltrust.Middleware.SCU.DE.DeutscheFiskal.Models
{
    public class UpdateTransactionRequestDto
    {
        [JsonProperty("clientId")]
        public string ClientId { get; set; }

        [JsonProperty("processType")]
        public string ProcessType { get; set; }

        [JsonProperty("processData")]
        public string ProcessData { get; set; }
    }
}