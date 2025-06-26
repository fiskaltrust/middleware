using Newtonsoft.Json;
using System;
using System.Runtime.Serialization;

namespace fiskaltrust.storage.serialization.V0
{
#if NET40 || NET35 || NET46 || NET461 || MONO40 || MONO35
    [Serializable]
#endif
    [DataContract]
    [JsonObject]
    public class ftCashBoxConfiguration
    {
        [JsonProperty(Required = Required.Default)]
        public PackageConfiguration[] helpers { get; set; }

        [JsonProperty]
        public Guid ftCashBoxId { get; set; }

        [JsonProperty]
        public PackageConfiguration[] ftSignaturCreationDevices { get; set; }

        [JsonProperty]
        public PackageConfiguration[] ftQueues { get; set; }

        [JsonProperty]
        public long TimeStamp { get; set; }

        public ftCashBoxConfiguration()
        {
            ftSignaturCreationDevices = new PackageConfiguration[] { };
            ftQueues = new PackageConfiguration[] { };
            helpers = null;
            TimeStamp = DateTime.UtcNow.Ticks;
        }

        public ftCashBoxConfiguration(Guid id) : this()
        {
            ftCashBoxId = id;
        }
    }
}