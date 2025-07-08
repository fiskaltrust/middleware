using Newtonsoft.Json;
using System;

namespace fiskaltrust.storage.serialization.V0
{
#if NET40 || NET35 || NET46 || NET461 || MONO40 || MONO35
    [Serializable]
#endif
    [JsonObject]
    public class TimeStampConfiguration
    {
        [JsonProperty]
        public long TimeStamp { get; set; }
    }
}