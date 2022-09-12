using fiskaltrust.Middleware.Contracts.Models;
using Newtonsoft.Json;

namespace fiskaltrust.Middleware.Localization.QueueDE
{
    public class QueueDEConfiguration
    {
        public bool FlagOptionalSignatures { get; set; } = true;

        [JsonProperty("scu-timeout-ms")]
        public long? ScuTimeoutMs { get; set; }

        [JsonProperty("scu-max-retries")]
        public int? ScuMaxRetries { get; set; }

        public bool StoreTemporaryExportFiles { get; set; } = false;

        public bool EnableTarFileExport { get; set; } = true;

        public TarFileExportMode TarFileExportMode { get; set; } = TarFileExportMode.All;

        public static QueueDEConfiguration FromMiddlewareConfiguration(MiddlewareConfiguration middlewareConfiguration) => JsonConvert.DeserializeObject<QueueDEConfiguration>(JsonConvert.SerializeObject(middlewareConfiguration.Configuration));
    }

    public enum TarFileExportMode
    {
        All,
        Erased
    }
}