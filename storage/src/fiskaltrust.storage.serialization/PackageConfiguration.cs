using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace fiskaltrust.storage.serialization.V0
{
#if NET40 || NET35 || NET46 || NET461 || MONO40 || MONO35
    [Serializable]
#endif
    [JsonObject]
    public class PackageConfiguration
    {
        [JsonProperty]
        public Guid Id { get; set; }

        [JsonProperty]
        public string Package { get; set; }

        [JsonProperty]
        public string Version { get; set; }

        [JsonProperty]
        public Dictionary<string, object> Configuration { get; set; }

        [JsonProperty(Required = Required.Default)]
        public string[] Url { get; set; }

        public PackageConfiguration()
        {
            Id = Guid.Empty;
            Package = string.Empty;
            Version = string.Empty;
            Configuration = null;
            Url = null;
        }
    }
}