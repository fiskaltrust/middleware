using System.Collections.Generic;
using Newtonsoft.Json;

namespace fiskaltrust.Middleware.Storage.MySQL
{
    public class MySQLStorageConfiguration
    {
        [JsonProperty("connectionstring")]
        public string ConnectionString;

        public static MySQLStorageConfiguration FromConfigurationDictionary(Dictionary<string, object> configuration) => JsonConvert.DeserializeObject<MySQLStorageConfiguration>(JsonConvert.SerializeObject(configuration));
    }
}