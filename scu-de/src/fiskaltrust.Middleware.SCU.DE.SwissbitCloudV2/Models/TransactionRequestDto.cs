using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace fiskaltrust.Middleware.SCU.DE.SwissbitCloudV2.Models
{
    public class TransactionRequestDto
    {
        [JsonProperty("clientId")]
        public string ClientId { get; set; }

        [JsonProperty("transactionNumber")]
        public int Number { get; set; }

        [JsonProperty("processData")]
        public string ProcessData { get; set; }

        [JsonProperty("processType")]
        public string ProcessType { get; set; }  
      
    }
}
