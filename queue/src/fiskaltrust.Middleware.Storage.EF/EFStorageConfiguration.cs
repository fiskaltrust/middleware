using System.Collections.Generic;
using Newtonsoft.Json;

namespace fiskaltrust.Middleware.Storage.EF
{
    public class EFStorageConfiguration
    {
        [JsonProperty("connectionstring")]
        public string ConnectionString;

        public static EFStorageConfiguration FromConfigurationDictionary(Dictionary<string, object> configuration) => JsonConvert.DeserializeObject<EFStorageConfiguration>(JsonConvert.SerializeObject(configuration));
    }
}