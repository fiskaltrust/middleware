using fiskaltrust.Middleware.Contracts.Models;
using Newtonsoft.Json;

namespace fiskaltrust.Middleware.Localization.QueueDE
{
    public class QueueATConfiguration
    {
        public bool FlagOptionalSignatures { get; set; } = true;
                
        [JsonProperty("scu-max-retries")]
        public int? ScuMaxRetries { get; set; } 

        public static QueueATConfiguration FromMiddlewareConfiguration(MiddlewareConfiguration middlewareConfiguration) 
            => JsonConvert.DeserializeObject<QueueATConfiguration>(JsonConvert.SerializeObject(middlewareConfiguration.Configuration));
    }
}