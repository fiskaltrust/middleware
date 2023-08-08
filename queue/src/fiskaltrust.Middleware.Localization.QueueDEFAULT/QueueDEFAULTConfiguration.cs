using fiskaltrust.Middleware.Contracts.Models;
using Newtonsoft.Json;

namespace fiskaltrust.Middleware.Localization.QueueDEFAULT
{
    public class QueueDEFAULTConfiguration
    {
        // Property to enable or disable the sandbox environment.
        // The default value is true.
        public bool Sandbox { get; set; } = true;

        // Static method to create a QueueDEFAULTConfiguration object from a MiddlewareConfiguration object.
        public static QueueDEFAULTConfiguration FromMiddlewareConfiguration(MiddlewareConfiguration middlewareConfiguration)
            => JsonConvert.DeserializeObject<QueueDEFAULTConfiguration>(JsonConvert.SerializeObject(middlewareConfiguration.Configuration));
    }
}