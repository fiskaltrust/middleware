using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace fiskaltrust.Middleware.SCU.DE.SwissbitCloudV2.Models
{
    public class TransactionResponseDto
    {
        [JsonProperty("transactionNumber")]
        public int Number { get; set; }

        [JsonProperty("signatureCounter")]
        public uint SignatureCounter { get; set; }

        [JsonProperty("signatureCreationTime")]
        public long SignatureCreationTime { get; set; }

        [JsonProperty("signatureValue")]
        public string SignatureValue { get; set; }  
      
    }
}
