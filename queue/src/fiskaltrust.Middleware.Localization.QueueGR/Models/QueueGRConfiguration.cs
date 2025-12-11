using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using fiskaltrust.Middleware.Localization.v2.Configuration;
using fiskaltrust.storage.V0;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace fiskaltrust.Middleware.Localization.QueueGR.Models;

public class QueueGRConfiguration
{
    [JsonProperty("scu-timeout-ms")]
    public long? ScuTimeoutMs { get; set; }

    [JsonProperty("scu-max-retries")]
    public int? ScuMaxRetries { get; set; }
    public static QueueGRConfiguration FromMiddlewareConfiguration(MiddlewareConfiguration middlewareConfiguration)
    => JsonConvert.DeserializeObject<QueueGRConfiguration>(JsonConvert.SerializeObject(middlewareConfiguration.Configuration));
            
}
