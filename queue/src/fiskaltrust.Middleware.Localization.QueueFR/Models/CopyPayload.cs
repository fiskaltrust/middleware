using Newtonsoft.Json;
using System;

namespace fiskaltrust.Middleware.Localization.QueueFR.Models
{
    public class CopyPayload : MinimumPayload
    {
        [JsonProperty("qid", DefaultValueHandling = DefaultValueHandling.Ignore, Required = Required.Default)]
        public Guid QueueId { get; set; }
        [JsonProperty("cbid", DefaultValueHandling = DefaultValueHandling.Ignore, Required = Required.Default)]
        public string CashBoxIdentification { get; set; }
        [JsonProperty("siret", DefaultValueHandling = DefaultValueHandling.Ignore, Required = Required.Default)]
        public string Siret { get; set; } = null;
        [JsonProperty("rid", DefaultValueHandling = DefaultValueHandling.Ignore, Required = Required.Default)]
        public string ReceiptId { get; set; }
        [JsonProperty("dt", DefaultValueHandling = DefaultValueHandling.Ignore, Required = Required.Default)]
        public DateTime ReceiptMoment { get; set; }
        [JsonProperty("qiid", DefaultValueHandling = DefaultValueHandling.Ignore, Required = Required.Default)]
        public Guid QueueItemId { get; set; }
        [JsonProperty("crr", DefaultValueHandling = DefaultValueHandling.Ignore, Required = Required.Default)]
        public string CopiedReceiptReference { get; set; }
        [JsonProperty("csn", DefaultValueHandling = DefaultValueHandling.Ignore, Required = Required.Default)]
        public string CertificateSerialNumber { get; set; }
    }
}
