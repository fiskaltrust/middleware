using System;
using Newtonsoft.Json;

namespace fiskaltrust.Middleware.SCU.DE.FiskalyCertified.Models
{
    public class ExportTransactions
    {
        [JsonProperty("client_id")]
        public Guid ClientId { get; set; }
    }
}
