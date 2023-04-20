using fiskaltrust.Middleware.Contracts.Models;
using Newtonsoft.Json;

namespace fiskaltrust.Middleware.Localization.QueueIT
{
    public class QueueITConfiguration
    {
        public bool Sandbox { get; set; } = false;
        public static QueueITConfiguration FromMiddlewareConfiguration(MiddlewareConfiguration middlewareConfiguration) => JsonConvert.DeserializeObject<QueueITConfiguration>(JsonConvert.SerializeObject(middlewareConfiguration.Configuration));
    }
}