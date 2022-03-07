using fiskaltrust.Middleware.Contracts.Models;
using Newtonsoft.Json;

namespace fiskaltrust.Middleware.Queue.PostgreSQL
{
    public class PostgreSQLQueueConfiguration
    {
        public static PostgreSQLQueueConfiguration FromMiddlewareConfiguration(MiddlewareConfiguration middlewareConfiguration) => JsonConvert.DeserializeObject<PostgreSQLQueueConfiguration>(JsonConvert.SerializeObject(middlewareConfiguration.Configuration));
    }
}