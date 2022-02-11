using System;
using Newtonsoft.Json;

namespace fiskaltrust.Middleware.Localization.QueueDE.Models
{
    public class FinishSCUSwitch
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
        public string TargetSCUPackageName { get; set; }

        [JsonProperty]
        public string TargetSCUSignatureAlgorithm { get; set; }

        [JsonProperty]
        public string TargetSCUPublicKeyBase64 { get; set; }

        [JsonProperty]
        public string TargetSCUSerialNumberOctet { get; set; }

        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore, Required = Required.Default)]
        public string Note { get; set; }

        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore, Required = Required.Default)]
        public string Version { get; set; }
    }
}
