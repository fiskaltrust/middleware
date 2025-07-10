using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;

namespace fiskaltrust.storage.serialization.DE.V0
{
    public class ActivateQueueSCU
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
        public Guid SCUId { get; set; }

        [JsonProperty]
        public string SCUPackageName { get; set; }

        [JsonProperty]
        public string SCUSignatureAlgorithm { get; set; }

        [JsonProperty]
        public string SCUPublicKeyBase64 { get; set; }

        [JsonProperty]
        public string SCUSerialNumberBase64 { get; set; }

        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore, Required = Required.Default)]
        public string Note { get; set; }

        [JsonProperty]
        public bool IsStartReceipt { get; set; }

        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore, Required = Required.Default)]
        public string Version { get; set; }
    }
}
