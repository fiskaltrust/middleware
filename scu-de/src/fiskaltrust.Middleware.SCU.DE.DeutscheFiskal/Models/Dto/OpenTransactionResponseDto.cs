using System;
using Newtonsoft.Json;

namespace fiskaltrust.Middleware.SCU.DE.DeutscheFiskal.Models
{
    public class OpenTransactionResponseDto
    {
        [JsonProperty("transactionNumber")]
        public int TransactionNumber { get; set; }
    }
}
