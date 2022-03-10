using System.Collections.Generic;
using Newtonsoft.Json;

namespace fiskaltrust.Middleware.Storage.EF
{
    public class EfStorageConfiguration
    {
        [JsonProperty("connectionstring")]
        public string ConnectionString;

        public static EfStorageConfiguration FromConfigurationDictionary(Dictionary<string, object> configuration) => JsonConvert.DeserializeObject<EfStorageConfiguration>(JsonConvert.SerializeObject(configuration));
    }
}