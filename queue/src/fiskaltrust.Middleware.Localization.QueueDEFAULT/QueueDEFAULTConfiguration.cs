using fiskaltrust.Middleware.Contracts.Models;
using Newtonsoft.Json;

namespace fiskaltrust.Middleware.Localization.QueueDEFAULT
{
    public class QueueDEFAULTConfiguration
    {
        public bool Sandbox { get; set; } = false;
        public static QueueDEFAULTConfiguration FromMiddlewareConfiguration(MiddlewareConfiguration middlewareConfiguration) => JsonConvert.DeserializeObject<QueueDEFAULTConfiguration>(JsonConvert.SerializeObject(middlewareConfiguration.Configuration));
    }
}