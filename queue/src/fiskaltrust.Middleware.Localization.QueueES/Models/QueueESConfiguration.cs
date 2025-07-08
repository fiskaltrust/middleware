using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using fiskaltrust.Middleware.Localization.v2.Configuration;
using fiskaltrust.storage.V0;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace fiskaltrust.Middleware.Localization.QueueES.Models;

public class QueueESConfiguration
{
    [JsonProperty("scu-timeout-ms")]
    public long? ScuTimeoutMs { get; set; }

    [JsonProperty("scu-max-retries")]
    public int? ScuMaxRetries { get; set; }
    public required string ScuUrl { get; set; }
    public static QueueESConfiguration FromMiddlewareConfiguration(MiddlewareConfiguration middlewareConfiguration)
    {
        var queueESConfiguration = JsonConvert.DeserializeObject<QueueESConfiguration>(JsonConvert.SerializeObject(middlewareConfiguration.Configuration));
        var key = "init_ftSignaturCreationUnitES";
        try
        {
            middlewareConfiguration.Configuration!.TryGetValue(key, out var value);
            var scus = JsonConvert.DeserializeObject<List<ftSignaturCreationUnitES>>(value!.ToString()!);
            queueESConfiguration.ScuUrl = scus.First().Url;
        }
        catch (Exception)
        {
            throw new ArgumentException($"Configuration must contain '{key}/Url'  parameter.");
        }       
        return queueESConfiguration;
    }
      
}
