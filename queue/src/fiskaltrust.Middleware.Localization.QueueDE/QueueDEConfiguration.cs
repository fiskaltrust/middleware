using fiskaltrust.Middleware.Contracts.Models;
using Newtonsoft.Json;

namespace fiskaltrust.Middleware.Localization.QueueDE
{
    public class QueueDEConfiguration
    {
        public bool FlagOptionalSignatures = true;
        
        [JsonProperty("scu-timeout-ms")]
        public long? ScuTimeoutMs;
        
        [JsonProperty("scu-max-retries")]
        public int? ScuMaxRetries;

        public bool StoreTemporaryExportFiles = false;
        
        public bool EnableTarFileExport = true;

        public static QueueDEConfiguration FromMiddlewareConfiguration(MiddlewareConfiguration middlewareConfiguration) => JsonConvert.DeserializeObject<QueueDEConfiguration>(JsonConvert.SerializeObject(middlewareConfiguration.Configuration));
    }
}