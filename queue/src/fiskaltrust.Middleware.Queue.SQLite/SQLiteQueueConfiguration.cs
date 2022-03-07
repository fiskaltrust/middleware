using fiskaltrust.Middleware.Contracts.Models;
using Newtonsoft.Json;

namespace fiskaltrust.Middleware.Queue.SQLite
{
    public class SQLiteQueueConfiguration
    {
        public static SQLiteQueueConfiguration FromMiddlewareConfiguration(MiddlewareConfiguration middlewareConfiguration) => JsonConvert.DeserializeObject<SQLiteQueueConfiguration>(JsonConvert.SerializeObject(middlewareConfiguration.Configuration));
    }
}