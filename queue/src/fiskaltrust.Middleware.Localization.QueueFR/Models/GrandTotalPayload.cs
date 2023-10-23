using Newtonsoft.Json;
using System;

namespace fiskaltrust.Middleware.Localization.QueueFR.Models
{
    public class GrandTotalPayload : MinimumPayload
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
        [JsonProperty("d-total", DefaultValueHandling = DefaultValueHandling.Ignore, Required = Required.Default)]
        public decimal? DTotalizer { get; set; } = null;
        [JsonProperty("d-ci-n", DefaultValueHandling = DefaultValueHandling.Ignore, Required = Required.Default)]
        public decimal? DCINormal { get; set; } = null;
        [JsonProperty("d-ci-r1", DefaultValueHandling = DefaultValueHandling.Ignore, Required = Required.Default)]
        public decimal? DCIReduced1 { get; set; } = null;
        [JsonProperty("d-ci-r2", DefaultValueHandling = DefaultValueHandling.Ignore, Required = Required.Default)]
        public decimal? DCIReduced2 { get; set; } = null;
        [JsonProperty("d-ci-rs", DefaultValueHandling = DefaultValueHandling.Ignore, Required = Required.Default)]
        public decimal? DCIReducedS { get; set; } = null;
        [JsonProperty("d-ci-z", DefaultValueHandling = DefaultValueHandling.Ignore, Required = Required.Default)]
        public decimal? DCIZero { get; set; } = null;
        [JsonProperty("d-ci-u", DefaultValueHandling = DefaultValueHandling.Ignore, Required = Required.Default)]
        public decimal? DCIUnknown { get; set; } = null;
        [JsonProperty("d-pi-c", DefaultValueHandling = DefaultValueHandling.Ignore, Required = Required.Default)]
        public decimal? DPICash { get; set; } = null;
        [JsonProperty("d-pi-n", DefaultValueHandling = DefaultValueHandling.Ignore, Required = Required.Default)]
        public decimal? DPINonCash { get; set; } = null;
        [JsonProperty("d-pi-i", DefaultValueHandling = DefaultValueHandling.Ignore, Required = Required.Default)]
        public decimal? DPIInternal { get; set; } = null;
        [JsonProperty("d-pi-u", DefaultValueHandling = DefaultValueHandling.Ignore, Required = Required.Default)]
        public decimal? DPIUnknown { get; set; } = null;
        [JsonProperty("m-total", DefaultValueHandling = DefaultValueHandling.Ignore, Required = Required.Default)]
        public decimal? MTotalizer { get; set; } = null;
        [JsonProperty("m-ci-n", DefaultValueHandling = DefaultValueHandling.Ignore, Required = Required.Default)]
        public decimal? MCINormal { get; set; } = null;
        [JsonProperty("m-ci-r1", DefaultValueHandling = DefaultValueHandling.Ignore, Required = Required.Default)]
        public decimal? MCIReduced1 { get; set; } = null;
        [JsonProperty("m-ci-r2", DefaultValueHandling = DefaultValueHandling.Ignore, Required = Required.Default)]
        public decimal? MCIReduced2 { get; set; } = null;
        [JsonProperty("m-ci-rs", DefaultValueHandling = DefaultValueHandling.Ignore, Required = Required.Default)]
        public decimal? MCIReducedS { get; set; } = null;
        [JsonProperty("m-ci-z", DefaultValueHandling = DefaultValueHandling.Ignore, Required = Required.Default)]
        public decimal? MCIZero { get; set; } = null;
        [JsonProperty("m-ci-u", DefaultValueHandling = DefaultValueHandling.Ignore, Required = Required.Default)]
        public decimal? MCIUnknown { get; set; } = null;
        [JsonProperty("m-pi-c", DefaultValueHandling = DefaultValueHandling.Ignore, Required = Required.Default)]
        public decimal? MPICash { get; set; } = null;
        [JsonProperty("m-pi-n", DefaultValueHandling = DefaultValueHandling.Ignore, Required = Required.Default)]
        public decimal? MPINonCash { get; set; } = null;
        [JsonProperty("m-pi-i", DefaultValueHandling = DefaultValueHandling.Ignore, Required = Required.Default)]
        public decimal? MPIInternal { get; set; } = null;
        [JsonProperty("m-pi-u", DefaultValueHandling = DefaultValueHandling.Ignore, Required = Required.Default)]
        public decimal? MPIUnknown { get; set; } = null;
        [JsonProperty("y-total", DefaultValueHandling = DefaultValueHandling.Ignore, Required = Required.Default)]
        public decimal? YTotalizer { get; set; } = null;
        [JsonProperty("y-ci-n", DefaultValueHandling = DefaultValueHandling.Ignore, Required = Required.Default)]
        public decimal? YCINormal { get; set; } = null;
        [JsonProperty("y-ci-r1", DefaultValueHandling = DefaultValueHandling.Ignore, Required = Required.Default)]
        public decimal? YCIReduced1 { get; set; } = null;
        [JsonProperty("y-ci-r2", DefaultValueHandling = DefaultValueHandling.Ignore, Required = Required.Default)]
        public decimal? YCIReduced2 { get; set; } = null;
        [JsonProperty("y-ci-rs", DefaultValueHandling = DefaultValueHandling.Ignore, Required = Required.Default)]
        public decimal? YCIReducedS { get; set; } = null;
        [JsonProperty("y-ci-z", DefaultValueHandling = DefaultValueHandling.Ignore, Required = Required.Default)]
        public decimal? YCIZero { get; set; } = null;
        [JsonProperty("y-ci-u", DefaultValueHandling = DefaultValueHandling.Ignore, Required = Required.Default)]
        public decimal? YCIUnknown { get; set; } = null;
        [JsonProperty("y-pi-c", DefaultValueHandling = DefaultValueHandling.Ignore, Required = Required.Default)]
        public decimal? YPICash { get; set; } = null;
        [JsonProperty("y-pi-n", DefaultValueHandling = DefaultValueHandling.Ignore, Required = Required.Default)]
        public decimal? YPINonCash { get; set; } = null;
        [JsonProperty("y-pi-i", DefaultValueHandling = DefaultValueHandling.Ignore, Required = Required.Default)]
        public decimal? YPIInternal { get; set; } = null;
        [JsonProperty("y-pi-u", DefaultValueHandling = DefaultValueHandling.Ignore, Required = Required.Default)]
        public decimal? YPIUnknown { get; set; } = null;
        [JsonProperty("s-total", DefaultValueHandling = DefaultValueHandling.Ignore, Required = Required.Default)]
        public decimal? STotalizer { get; set; } = null;
        [JsonProperty("s-ci-n", DefaultValueHandling = DefaultValueHandling.Ignore, Required = Required.Default)]
        public decimal? SCINormal { get; set; } = null;
        [JsonProperty("s-ci-r1", DefaultValueHandling = DefaultValueHandling.Ignore, Required = Required.Default)]
        public decimal? SCIReduced1 { get; set; } = null;
        [JsonProperty("s-ci-r2", DefaultValueHandling = DefaultValueHandling.Ignore, Required = Required.Default)]
        public decimal? SCIReduced2 { get; set; } = null;
        [JsonProperty("s-ci-rs", DefaultValueHandling = DefaultValueHandling.Ignore, Required = Required.Default)]
        public decimal? SCIReducedS { get; set; } = null;
        [JsonProperty("s-ci-z", DefaultValueHandling = DefaultValueHandling.Ignore, Required = Required.Default)]
        public decimal? SCIZero { get; set; } = null;
        [JsonProperty("s-ci-u", DefaultValueHandling = DefaultValueHandling.Ignore, Required = Required.Default)]
        public decimal? SCIUnknown { get; set; } = null;
        [JsonProperty("s-pi-c", DefaultValueHandling = DefaultValueHandling.Ignore, Required = Required.Default)]
        public decimal? SPICash { get; set; } = null;
        [JsonProperty("s-pi-n", DefaultValueHandling = DefaultValueHandling.Ignore, Required = Required.Default)]
        public decimal? SPINonCash { get; set; } = null;
        [JsonProperty("s-pi-i", DefaultValueHandling = DefaultValueHandling.Ignore, Required = Required.Default)]
        public decimal? SPIInternal { get; set; } = null;
        [JsonProperty("s-pi-u", DefaultValueHandling = DefaultValueHandling.Ignore, Required = Required.Default)]
        public decimal? SPIUnknown { get; set; } = null;
        [JsonProperty("csn", DefaultValueHandling = DefaultValueHandling.Ignore, Required = Required.Default)]
        public string CertificateSerialNumber { get; set; }
    }
}
