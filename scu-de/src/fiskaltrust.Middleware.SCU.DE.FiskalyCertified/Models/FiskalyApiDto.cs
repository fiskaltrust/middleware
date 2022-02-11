using System;
using Newtonsoft.Json;

namespace fiskaltrust.Middleware.SCU.DE.FiskalyCertified.Models
{
    public class FiskalyApiDto
    {
        [JsonProperty("_id")]
        public Guid Id { get; set; }

        [JsonProperty("_type")]
        public string Type { get; set; }

        [JsonProperty("_env")]
        public string Env { get; set; }

        [JsonProperty("_version")]
        public string Version { get; set; }
    }
}
