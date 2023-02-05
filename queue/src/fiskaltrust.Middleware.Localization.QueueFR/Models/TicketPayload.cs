using Newtonsoft.Json;
using System;

namespace fiskaltrust.Middleware.Localization.QueueFR.Models
{
    public class TicketPayload : MinimumPayload
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
        [JsonProperty("total", DefaultValueHandling = DefaultValueHandling.Ignore, Required = Required.Default)]
        public decimal? Totalizer { get; set; } = null;
        [JsonProperty("ci-n", DefaultValueHandling = DefaultValueHandling.Ignore, Required = Required.Default)]
        public decimal? CINormal { get; set; } = null;
        [JsonProperty("ci-r1", DefaultValueHandling = DefaultValueHandling.Ignore, Required = Required.Default)]
        public decimal? CIReduced1 { get; set; } = null;
        [JsonProperty("ci-r2", DefaultValueHandling = DefaultValueHandling.Ignore, Required = Required.Default)]
        public decimal? CIReduced2 { get; set; } = null;
        [JsonProperty("ci-rs", DefaultValueHandling = DefaultValueHandling.Ignore, Required = Required.Default)]
        public decimal? CIReducedS { get; set; } = null;
        [JsonProperty("ci-z", DefaultValueHandling = DefaultValueHandling.Ignore, Required = Required.Default)]
        public decimal? CIZero { get; set; } = null;
        [JsonProperty("ci-u", DefaultValueHandling = DefaultValueHandling.Ignore, Required = Required.Default)]
        public decimal? CIUnknown { get; set; } = null;
        [JsonProperty("pi-c", DefaultValueHandling = DefaultValueHandling.Ignore, Required = Required.Default)]
        public decimal? PICash { get; set; } = null;
        [JsonProperty("pi-n", DefaultValueHandling = DefaultValueHandling.Ignore, Required = Required.Default)]
        public decimal? PINonCash { get; set; } = null;
        [JsonProperty("pi-i", DefaultValueHandling = DefaultValueHandling.Ignore, Required = Required.Default)]
        public decimal? PIInternal { get; set; } = null;
        [JsonProperty("pi-u", DefaultValueHandling = DefaultValueHandling.Ignore, Required = Required.Default)]
        public decimal? PIUnknown { get; set; } = null;
        [JsonProperty("csn", DefaultValueHandling = DefaultValueHandling.Ignore, Required = Required.Default)]
        public string CertificateSerialNumber { get; set; }
    }
}
