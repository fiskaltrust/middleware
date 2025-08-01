using fiskaltrust.Middleware.Contracts.Models;
using Newtonsoft.Json;

namespace fiskaltrust.Middleware.Localization.QueuePT.Models;

public class QueuePTConfiguration
{
    public bool Sandbox { get; set; } = false;

    public static QueuePTConfiguration FromMiddlewareConfiguration(MiddlewareConfiguration middlewareConfiguration) => JsonConvert.DeserializeObject<QueuePTConfiguration>(JsonConvert.SerializeObject(middlewareConfiguration.Configuration));
}