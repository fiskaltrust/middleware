using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace fiskaltrust.Middleware.SCU.DE.FiskalyCertified.Models
{
    public class ClientDto : FiskalyApiDto
    {
        [JsonProperty("serial_number")]
        public string SerialNumber { get; set; }

        [JsonProperty("state")]
        public string State { get; set; }

        [JsonProperty("time_creation")]
        public int TimeCreation { get; set; }

        [JsonProperty("time_update")]
        public int TimeUpdate { get; set; }

        [JsonProperty("tss_id")]
        public Guid TssId { get; set; }

        [JsonProperty("metadata")]
        public Dictionary<string, object> metadata { get; set; }
    }
}
