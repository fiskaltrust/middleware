using System;
using System.Linq;
using fiskaltrust.Middleware.Contracts.Models;
using Microsoft.Extensions.Logging;
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

        public bool DisableDsfinvkExportReferences { get; set; } = false;

        private bool EnableTarFileExport { get; set; } = true;

        public TarFileExportMode TarFileExportMode { get; set; } = TarFileExportMode.All;

        public bool ExcludeDsfinvkOrders { get; set; } = false;

        public static QueueDEConfiguration FromMiddlewareConfiguration(ILogger<QueueDEConfiguration> logger, MiddlewareConfiguration middlewareConfiguration)
        {
            var configuration = JsonConvert.DeserializeObject<QueueDEConfiguration>(JsonConvert.SerializeObject(middlewareConfiguration.Configuration));

            var enableTarFileExportPair = middlewareConfiguration.Configuration.FirstOrDefault(k => k.Key.ToLower() == nameof(EnableTarFileExport).ToLower());
            bool? enableTarFileExport = string.IsNullOrEmpty(enableTarFileExportPair.Value?.ToString()) ? null : bool.Parse(enableTarFileExportPair.Value.ToString());
            var tarFileExportModePair = middlewareConfiguration.Configuration.FirstOrDefault(k => k.Key.ToLower() == nameof(TarFileExportMode).ToLower());

            if (enableTarFileExport.HasValue && !string.IsNullOrEmpty(tarFileExportModePair.Value?.ToString()))
            {
                logger.LogWarning($"Both {nameof(EnableTarFileExport)} and {nameof(TarFileExportMode)} are set. {nameof(TarFileExportMode)} = {configuration.TarFileExportMode} is choosen.");
            }
            else if (enableTarFileExport.HasValue)
            {
                configuration.TarFileExportMode = enableTarFileExport.Value switch
                {
                    true => TarFileExportMode.All,
                    false => TarFileExportMode.None,
                };
            }

            return configuration;
        }
    }

    public enum TarFileExportMode
    {
        None,
        All,
        Erased
    }
}