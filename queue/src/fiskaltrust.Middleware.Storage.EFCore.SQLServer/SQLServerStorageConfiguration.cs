using System.Collections.Generic;
using Newtonsoft.Json;

namespace fiskaltrust.Middleware.Storage.EFCore.SQLServer
{
    
    public class SQLServerStorageConfiguration
    {
        [JsonProperty("connectionstring")]
        public string ConnectionString { get; set; }

        public static SQLServerStorageConfiguration FromConfigurationDictionary(Dictionary<string, object> configuration) 
            => JsonConvert.DeserializeObject<SQLServerStorageConfiguration>(JsonConvert.SerializeObject(configuration));
    }
}