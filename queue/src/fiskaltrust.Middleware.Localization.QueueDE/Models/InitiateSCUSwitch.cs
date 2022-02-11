using System;
using Newtonsoft.Json;

namespace fiskaltrust.Middleware.Localization.QueueDE.Models
{
    public class InitiateSCUSwitch
    {
        [JsonProperty]
        public Guid CashBoxId { get; set; }

        [JsonProperty]
        public Guid QueueId { get; set; }

        [JsonProperty]
        public DateTime Moment { get; set; }

        [JsonProperty]
        public string CashBoxIdentification { get; set; }

        [JsonProperty]
        public Guid SourceSCUId { get; set; }

        [JsonProperty]
        public Guid TargetSCUId { get; set; }

        [JsonProperty]
        public string SourceSCUPackageName { get; set; }

        [JsonProperty]
        public string SourceSCUSignatureAlgorithm { get; set; }

        [JsonProperty]
        public string SourceSCUPublicKeyBase64 { get; set; }

        [JsonProperty]
        public string SourceSCUSerialNumberOctet { get; set; }

        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore, Required = Required.Default)]
        public string Note { get; set; }

        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore, Required = Required.Default)]
        public string Version { get; set; }
    }
}
