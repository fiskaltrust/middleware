using Newtonsoft.Json;
using System;

namespace fiskaltrust.storage.serialization.AT.V0
{
    public class FonDeactivateSCU
    {
        [JsonProperty]
        public Guid CashBoxId { get; set; }

        [JsonProperty]
        public Guid SCUId { get; set; }

        [JsonProperty]
        public string PackageName { get; set; }

        [JsonProperty]
        public DateTime Moment { get; set; }

        [JsonProperty]
        public string VDA { get; set; }

        [JsonProperty]
        public string SerialNumber { get; set; }

        [JsonProperty]
        public bool Temporary { get; set; }

        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore, Required = Required.Default)]
        public string CertificateBase64 { get; set; }

        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore, Required = Required.Default)]
        public string ClosedSystemKind { get; set; }

        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore, Required = Required.Default)]
        public string ClosedSystemValue { get; set; }

        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore, Required = Required.Default)]
        public string Note { get; set; }

        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore, Required = Required.Default)]
        public string Version { get; set; }
    }
}