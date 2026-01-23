using System.Collections.Generic;
using Newtonsoft.Json;

namespace fiskaltrust.Middleware.Storage.EF
{
    public class EfStorageConfiguration
    {
        [JsonProperty("connectionstring")]
        public string ConnectionString { get; set; }

        public int MigrationsTimeoutSec { get; set; } = 30 * 60;

        public int SqlCommandTimeoutSec { get; set; } = 3 * 60;

        public static EfStorageConfiguration FromConfigurationDictionary(Dictionary<string, object> configuration) => JsonConvert.DeserializeObject<EfStorageConfiguration>(JsonConvert.SerializeObject(configuration));
    }
}