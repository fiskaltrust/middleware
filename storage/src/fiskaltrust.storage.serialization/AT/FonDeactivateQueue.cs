using Newtonsoft.Json;
using System;

namespace fiskaltrust.storage.serialization.AT.V0
{
    public class FonDeactivateQueue
    {
        [JsonProperty]
        public Guid CashBoxId { get; set; }

        [JsonProperty]
        public Guid QueueId { get; set; }

        [JsonProperty]
        public DateTime Moment { get; set; }

        [JsonProperty]
        public string CashBoxIdentification { get; set; }

        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore, Required = Required.Default)]
        public string ClosedSystemKind { get; set; }

        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore, Required = Required.Default)]
        public string ClosedSystemValue { get; set; }

        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore, Required = Required.Default)]
        public string Note { get; set; }

        [JsonProperty]
        public string DEPValue { get; set; }

        [JsonProperty]
        public bool IsStopReceipt { get; set; }

        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore, Required = Required.Default)]
        public string Version { get; set; }
    }
}