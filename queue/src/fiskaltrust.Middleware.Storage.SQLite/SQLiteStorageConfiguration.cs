using System.Collections.Generic;
using Newtonsoft.Json;

namespace fiskaltrust.Middleware.Storage.SQLite
{
    public class SQLiteStorageConfiguration
    {
        public int MigrationsTimeoutSec { get; set; } = 30 * 60;

        public static SQLiteStorageConfiguration FromConfigurationDictionary(Dictionary<string, object> configuration) => JsonConvert.DeserializeObject<SQLiteStorageConfiguration>(JsonConvert.SerializeObject(configuration));
    }
}