using fiskaltrust.Middleware.Contracts.Models;
using Newtonsoft.Json;

namespace fiskaltrust.Middleware.Localization.QueueES
{
    public class QueueESConfiguration
    {
        public bool Sandbox { get; set; } = false;
        public static QueueESConfiguration FromMiddlewareConfiguration(MiddlewareConfiguration middlewareConfiguration) => JsonConvert.DeserializeObject<QueueESConfiguration>(JsonConvert.SerializeObject(middlewareConfiguration.Configuration));
    }
}