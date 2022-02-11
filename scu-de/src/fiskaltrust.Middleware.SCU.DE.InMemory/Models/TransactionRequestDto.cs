using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace fiskaltrust.Middleware.SCU.DE.InMemory.Models
{
    public class TransactionRequestDto
    {
        [JsonProperty("state")]
        public string State { get; set; }

        [JsonProperty("client_id")]
        public string ClientId { get; set; }

        [JsonProperty("schema")]
        public TransactionDataDto Data { get; set; }

    }
}
