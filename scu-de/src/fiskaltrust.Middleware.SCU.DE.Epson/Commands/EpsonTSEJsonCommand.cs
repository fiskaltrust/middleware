using System.Collections.Generic;
using Newtonsoft.Json;

namespace fiskaltrust.Middleware.SCU.DE.Epson
{
    public class EpsonTSEJsonCommand
    {
        [JsonProperty("storage")]
        public StoragePayload Storage { get; set; }

        [JsonProperty("function")]
        public string Function { get; set; }

        [JsonProperty("compress")]
        public CompressSettingsPayload Compress { get; set; } = new CompressSettingsPayload();

        [JsonProperty("input")]
        public Dictionary<string, object> Input { get; set; } = new Dictionary<string, object>();

        public EpsonTSEJsonCommand()
        {

        }

        public EpsonTSEJsonCommand(string function): this(function, StoragePayload.CreateTSE()) { }

        public EpsonTSEJsonCommand(string function, StoragePayload storage)
        {
            Function = function;
            Storage = storage;
        }
    }

    public class CompressSettingsPayload
    {
        [JsonProperty("required")]
        public bool Required { get; set; } = false;

        [JsonProperty("type")]
        public string Type { get; set; }
    }

    public class StoragePayload
    {
        [JsonProperty("type")]
        public string Type { get; set; } = "TSE";

        [JsonProperty("vendor")]
        public string Vendor { get; set; }

        public static StoragePayload CreateCommon() =>
             new StoragePayload
             {
                 Type = "COMMON"
             };
        public static StoragePayload CreateTSE() =>
             new StoragePayload
             {
                 Type = "TSE"
             };
    }

    public class EpsonResultPayload<T>
    {
        public string Result { get; set; }

        public string Function { get; set; }

        public Dictionary<string, string> Error { get; }

        public T Output { get; set; }
    }
}
