using System;
using Newtonsoft.Json;

namespace fiskaltrust.Middleware.SCU.DE.FiskalyCertified.Models
{
    public class ExportStateInformationDto : FiskalyApiDto
    {
        [JsonProperty("href")]
        public string Href { get; set; }

        [JsonProperty("state")]
        public string State { get; set; }

        [JsonProperty("exception")]
        public string Exception { get; set; }

        [JsonProperty("estimated_time_of_completion")]
        public long EstimatedTimeOfCompletion { get; set; }

        [JsonProperty("time_request")]
        public long TimeRequest { get; set; }

        [JsonProperty("time_start")]
        public long TimeStart { get; set; }

        [JsonProperty("time_end")]
        public long TimeEnd { get; set; }

        [JsonProperty("time_expiration")]
        public long TimeExpiration { get; set; }

        [JsonProperty("time_error")]
        public long TimeError { get; set; }

        [JsonProperty("tss_id")]
        public Guid TssId { get; set; }

        public bool IsExportDone() =>  State == "COMPLETED" || State == "ERROR" || State == "CANCELLED" || State == "EXPIRED" || State == "DELETED";
    }
}
