using Newtonsoft.Json;

namespace fiskaltrust.Middleware.SCU.DE.InMemory.Models
{
    public class RawData
    {
        [JsonProperty("process_data")]
        public string ProcessData { get; set; }

        [JsonProperty("process_type")]
        public string ProcessType { get; set; }
    }
}
