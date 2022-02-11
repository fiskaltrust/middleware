using System;
using Newtonsoft.Json;

namespace fiskaltrust.Middleware.SCU.DE.InMemory.Models
{
    public class ClientDto 
    {
        [JsonProperty("_id")]
        public Guid Id { get; set; }

        [JsonProperty("serial_number")]
        public string SerialNumber { get; set; }

        [JsonProperty("time_creation")]
        public int TimeCreation { get; set; }

        [JsonProperty("tss_id")]
        public Guid TssId { get; set; }
    }
}
