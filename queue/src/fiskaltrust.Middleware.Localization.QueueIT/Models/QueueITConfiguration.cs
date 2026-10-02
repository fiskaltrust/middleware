using fiskaltrust.Middleware.Localization.v2.Configuration;
using Newtonsoft.Json;

namespace fiskaltrust.Middleware.Localization.QueueIT.Models;

public class QueueITConfiguration
{
    [JsonProperty("scu-timeout-ms")]
    public long? ScuTimeoutMs { get; set; }

    // SKE => currently we don't perform any retries, we'll have to think about how we can handle this differently in the future, probably letting one of either component decide
    //        also this thing has to be 1 since we are considering the first try also as retry.
    [JsonProperty("scu-max-retries")]
    public int? ScuMaxRetries { get; set; } = 1;

    public static QueueITConfiguration FromMiddlewareConfiguration(MiddlewareConfiguration middlewareConfiguration)
        => JsonConvert.DeserializeObject<QueueITConfiguration>(JsonConvert.SerializeObject(middlewareConfiguration.Configuration)) ?? new QueueITConfiguration();
}
