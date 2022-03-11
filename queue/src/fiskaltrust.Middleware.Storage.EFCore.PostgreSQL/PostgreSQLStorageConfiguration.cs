using System.Collections.Generic;
using Newtonsoft.Json;

namespace fiskaltrust.Middleware.Storage.EFCore.PostgreSQL
{
    public class PostgreSQLStorageConfiguration
    {
        [JsonProperty("connectionstring")]
        public string ConnectionString { get; set; }

        public static PostgreSQLStorageConfiguration FromConfigurationDictionary(Dictionary<string, object> configuration) => JsonConvert.DeserializeObject<PostgreSQLStorageConfiguration>(JsonConvert.SerializeObject(configuration));
    }
}