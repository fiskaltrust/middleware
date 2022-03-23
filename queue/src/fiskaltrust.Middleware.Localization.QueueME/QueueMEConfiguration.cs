using fiskaltrust.Middleware.Contracts.Models;
using Newtonsoft.Json;

namespace fiskaltrust.Middleware.Localization.QueueME
{
    public class QueueMEConfiguration
    {
        public static QueueMEConfiguration FromMiddlewareConfiguration(MiddlewareConfiguration middlewareConfiguration) => JsonConvert.DeserializeObject<QueueMEConfiguration>(JsonConvert.SerializeObject(middlewareConfiguration.Configuration));
    }
}