using System;
using Newtonsoft.Json;

namespace fiskaltrust.Middleware.SCU.DE.FiskalyCertified.Models
{
    public class ExportTransactionsWithDatesDto
    {
        [JsonProperty("client_id")]
        public Guid ClientId { get; set; }

        [JsonProperty("start_date")]
        public ulong StartDate { get; set; }

        [JsonProperty("end_date")]
        public ulong EndDate { get; set; }
    }
}
