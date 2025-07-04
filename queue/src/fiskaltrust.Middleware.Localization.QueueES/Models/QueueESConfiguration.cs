using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using fiskaltrust.Middleware.Localization.v2.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace fiskaltrust.Middleware.Localization.QueueES.Models;

public class QueueESConfiguration
{
    [JsonProperty("scu-timeout-ms")]
    public long? ScuTimeoutMs { get; set; }

    [JsonProperty("scu-max-retries")]
    public int? ScuMaxRetries { get; set; }
    public static QueueESConfiguration FromMiddlewareConfiguration(MiddlewareConfiguration middlewareConfiguration) =>
    JsonConvert.DeserializeObject<QueueESConfiguration>(JsonConvert.SerializeObject(middlewareConfiguration.Configuration));
    
}
