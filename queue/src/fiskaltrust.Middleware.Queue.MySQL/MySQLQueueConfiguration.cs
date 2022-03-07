using fiskaltrust.Middleware.Contracts.Models;
using Newtonsoft.Json;

namespace fiskaltrust.Middleware.Queue.MySQL
{
    public class MySQLQueueConfiguration
    {
        public static MySQLQueueConfiguration FromMiddlewareConfiguration(MiddlewareConfiguration middlewareConfiguration) => JsonConvert.DeserializeObject<MySQLQueueConfiguration>(JsonConvert.SerializeObject(middlewareConfiguration.Configuration));
    }
}