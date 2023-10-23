using Newtonsoft.Json;

namespace fiskaltrust.Middleware.Localization.QueueFR.Models
{
    public class MinimumPayload
    {
        [JsonProperty("rc", DefaultValueHandling = DefaultValueHandling.Ignore, Required = Required.Default)]
        public long ReceiptCase { get; set; }
        [JsonProperty("hash", DefaultValueHandling = DefaultValueHandling.Ignore, Required = Required.Default)]
        public string LastHash { get; set; } = null;
    }
}
