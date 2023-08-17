using fiskaltrust.Middleware.Contracts.Models;
using Newtonsoft.Json;

namespace fiskaltrust.Middleware.Localization.QueueDEFAULT
{
    /// <summary>
    /// Represents the configuration specific to the DEFAULT queue.
    /// </summary>
    /// <remarks>
    /// This class encapsulates the properties and methods needed for configuring the DEFAULT queue.
    /// Different queue configuration settings can be added to this class if required by the specific market.
    /// Currently, it includes a property to enable or disable the sandbox environment.
    /// </remarks>
    public class QueueDEFAULTConfiguration
    {
        /// <summary>
        /// Gets or sets a value indicating whether the sandbox environment is enabled or disabled. The default value is true.
        /// </summary>
        public bool Sandbox { get; set; } = true;

        /// <summary>
        /// Creates a QueueDEFAULTConfiguration object from a MiddlewareConfiguration object.
        /// </summary>
        public static QueueDEFAULTConfiguration FromMiddlewareConfiguration(MiddlewareConfiguration middlewareConfiguration)
            => JsonConvert.DeserializeObject<QueueDEFAULTConfiguration>(JsonConvert.SerializeObject(middlewareConfiguration.Configuration));
    }
}