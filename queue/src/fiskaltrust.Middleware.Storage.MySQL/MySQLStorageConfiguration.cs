﻿using System.Collections.Generic;
using Newtonsoft.Json;

namespace fiskaltrust.Middleware.Storage.MySQL
{
    public class MySQLStorageConfiguration
    {
        [JsonProperty("connectionstring")]
        public string ConnectionString { get; set; }
        
        [JsonProperty("maxpoolsize")]
        public uint MaxPoolSize { get; set; } = 500;
        public uint MigrationsTimeoutSec { get; set; } = 30 * 60;

        public static MySQLStorageConfiguration FromConfigurationDictionary(Dictionary<string, object> configuration) => JsonConvert.DeserializeObject<MySQLStorageConfiguration>(JsonConvert.SerializeObject(configuration));
    }
}