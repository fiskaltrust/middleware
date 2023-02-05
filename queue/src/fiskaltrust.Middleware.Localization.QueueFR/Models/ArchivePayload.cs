using Newtonsoft.Json;
using System;

namespace fiskaltrust.Middleware.Localization.QueueFR.Models
{
    public class ArchivePayload : GrandTotalPayload
    {
        [JsonProperty("a-total", DefaultValueHandling = DefaultValueHandling.Ignore, Required = Required.Default)]
        public decimal? ATotalizer { get; set; } = null;
        
        [JsonProperty("a-ci-n", DefaultValueHandling = DefaultValueHandling.Ignore, Required = Required.Default)]
        public decimal? ACINormal { get; set; } = null;
        
        [JsonProperty("a-ci-r1", DefaultValueHandling = DefaultValueHandling.Ignore, Required = Required.Default)]
        public decimal? ACIReduced1 { get; set; } = null;
       
        [JsonProperty("a-ci-r2", DefaultValueHandling = DefaultValueHandling.Ignore, Required = Required.Default)]
        public decimal? ACIReduced2 { get; set; } = null;
       
        [JsonProperty("a-ci-rs", DefaultValueHandling = DefaultValueHandling.Ignore, Required = Required.Default)]
        public decimal? ACIReducedS { get; set; } = null;
       
        [JsonProperty("a-ci-z", DefaultValueHandling = DefaultValueHandling.Ignore, Required = Required.Default)]
        public decimal? ACIZero { get; set; } = null;
       
        [JsonProperty("a-ci-u", DefaultValueHandling = DefaultValueHandling.Ignore, Required = Required.Default)]
        public decimal? ACIUnknown { get; set; } = null;
       
        [JsonProperty("a-pi-c", DefaultValueHandling = DefaultValueHandling.Ignore, Required = Required.Default)]
        public decimal? APICash { get; set; } = null;
      
        [JsonProperty("a-pi-n", DefaultValueHandling = DefaultValueHandling.Ignore, Required = Required.Default)]
        public decimal? APINonCash { get; set; } = null;
      
        [JsonProperty("a-pi-i", DefaultValueHandling = DefaultValueHandling.Ignore, Required = Required.Default)]
        public decimal? APIInternal { get; set; } = null;
      
        [JsonProperty("a-pi-u", DefaultValueHandling = DefaultValueHandling.Ignore, Required = Required.Default)]
        public decimal? APIUnknown { get; set; } = null;
     
        [JsonProperty("lajid", DefaultValueHandling = DefaultValueHandling.Ignore, Required = Required.Default)]
        public Guid LastActionJournalId { get; set; }
      
        [JsonProperty("ljid", DefaultValueHandling = DefaultValueHandling.Ignore, Required = Required.Default)]
        public Guid LastJournalFRId { get; set; }
      
        [JsonProperty("lrjid", DefaultValueHandling = DefaultValueHandling.Ignore, Required = Required.Default)]
        public Guid LastReceiptJournalId { get; set; }
     
        [JsonProperty("paqiid", DefaultValueHandling = DefaultValueHandling.Ignore, Required = Required.Default)]
        public Guid? PreviousArchiveQueueItemId { get; set; }
    
        [JsonProperty("fcrqiid", DefaultValueHandling = DefaultValueHandling.Ignore, Required = Required.Default)]
        public Guid? FirstContainedReceiptQueueItemId { get; set; }
    
        [JsonProperty("fcrm", DefaultValueHandling = DefaultValueHandling.Ignore, Required = Required.Default)]
        public DateTime? FirstContainedReceiptMoment { get; set; }
     
        [JsonProperty("lcrqiid", DefaultValueHandling = DefaultValueHandling.Ignore, Required = Required.Default)]
        public Guid? LastContainedReceiptQueueItemId { get; set; }
     
        [JsonProperty("lcrm", DefaultValueHandling = DefaultValueHandling.Ignore, Required = Required.Default)]
        public DateTime? LastContainedReceiptMoment { get; set; }
    }
}
