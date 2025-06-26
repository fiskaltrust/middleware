using Newtonsoft.Json;
using System;

namespace fiskaltrust.storage.serialization.AT.V0
{
    public class FonVerifySignature
    {
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore, Required = Required.Default)]
        public Guid CashBoxId { get; set; }

        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore, Required = Required.Default)]
        public Guid QueueId { get; set; }

        [JsonProperty]
        public string DEPValue { get; set; }

        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore, Required = Required.Default)]
        public string ClosedSystemKind { get; set; }

        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore, Required = Required.Default)]
        public string ClosedSystemValue { get; set; }

        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore, Required = Required.Default)]
        public Guid SCUId { get; set; }

        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore, Required = Required.Default)]
        public string CertificateBase64 { get; set; }

        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore, Required = Required.Default)]
        public string CashBoxIdentification { get; set; }

        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore, Required = Required.Default)]
        public string CashBoxKeyBase64 { get; set; }

        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore, Required = Required.Default)]
        public string Version { get; set; }
    }
}