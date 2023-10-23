using fiskaltrust.Middleware.Contracts.Models;
using Newtonsoft.Json;

namespace fiskaltrust.Middleware.Localization.QueueME
{
    public class QueueMEConfiguration
    {
        public bool Sandbox { get; set; } = false;
        public static QueueMEConfiguration FromMiddlewareConfiguration(MiddlewareConfiguration middlewareConfiguration) => JsonConvert.DeserializeObject<QueueMEConfiguration>(JsonConvert.SerializeObject(middlewareConfiguration.Configuration));
    }
}