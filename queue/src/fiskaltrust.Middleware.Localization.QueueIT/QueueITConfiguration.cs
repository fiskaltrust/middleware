using fiskaltrust.Middleware.Contracts.Models;
using Newtonsoft.Json;

namespace fiskaltrust.Middleware.Localization.QueueIT
{
    public class QueueITConfiguration
    {
        public bool Sandbox { get; set; } = false;


        [JsonProperty("scu-timeout-ms")]
        public long? ScuTimeoutMs { get; set; }

        [JsonProperty("scu-max-retries")]
        public int? ScuMaxRetries { get; set; } = 0; // SKE => currently we don't perform any retries, we'll have to think about how we can handle this differently in the future, probably letting one of either component decide

        public static QueueITConfiguration FromMiddlewareConfiguration(MiddlewareConfiguration middlewareConfiguration) => JsonConvert.DeserializeObject<QueueITConfiguration>(JsonConvert.SerializeObject(middlewareConfiguration.Configuration));
    }
}