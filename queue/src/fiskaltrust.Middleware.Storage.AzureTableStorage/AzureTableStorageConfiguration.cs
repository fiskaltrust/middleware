using System.Collections.Generic;
using Newtonsoft.Json;

namespace fiskaltrust.Middleware.Storage.AzureTableStorage
{
    public class AzureTableStorageConfiguration
    {
        [JsonProperty("connectionstring")]
        [System.Text.Json.Serialization.JsonPropertyName("connectionstring")]
        public string ConnectionString { get; set; }

        [JsonProperty("storageconnectionstring")]
        [System.Text.Json.Serialization.JsonPropertyName("storageconnectionstring")]
        public string StorageConnectionString { get; set; }

        [JsonProperty("storageaccountname")]
        [System.Text.Json.Serialization.JsonPropertyName("storageaccountname")]
        public string StorageAccountName { get; set; }

        public static AzureTableStorageConfiguration FromConfigurationDictionary(Dictionary<string, object> configuration) => JsonConvert.DeserializeObject<AzureTableStorageConfiguration>(JsonConvert.SerializeObject(configuration));
    }
}
