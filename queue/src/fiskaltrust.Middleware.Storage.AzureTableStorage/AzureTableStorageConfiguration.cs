using System.Collections.Generic;
using Newtonsoft.Json;

namespace fiskaltrust.Middleware.Storage.AzureTableStorage
{
    public class AzureTableStorageConfiguration
    {
        [JsonProperty("storageconnectionstring")]
        public string StorageConnectionString { get; set; }

        [JsonProperty("storageaccountname")]
        public string StorageAccountName { get; set; }

        public static AzureTableStorageConfiguration FromConfigurationDictionary(Dictionary<string, object> configuration) => JsonConvert.DeserializeObject<AzureTableStorageConfiguration>(JsonConvert.SerializeObject(configuration));
    }
}
