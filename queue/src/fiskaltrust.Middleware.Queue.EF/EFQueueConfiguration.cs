using fiskaltrust.Middleware.Contracts.Models;
using Newtonsoft.Json;

namespace fiskaltrust.Middleware.Queue.EF
{
    public class EFQueueConfiguration
    {
        public static EFQueueConfiguration FromMiddlewareConfiguration(MiddlewareConfiguration middlewareConfiguration) => JsonConvert.DeserializeObject<EFQueueConfiguration>(JsonConvert.SerializeObject(middlewareConfiguration.Configuration));
    }
}