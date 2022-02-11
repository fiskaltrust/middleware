using Newtonsoft.Json;

namespace fiskaltrust.Middleware.SCU.DE.DeutscheFiskal.Models
{
    public class FccInfoResponseDto
    {
        [JsonProperty("maxNumberClients")]
        public long MaxNumberClients { get; set; }

        [JsonProperty("maxNumberTransactions")]
        public long MaxNumberTransactions { get; set; }

        [JsonProperty("supportedUpdateVariant")]
        public string SupportedUpdateVariant { get; set; }

        [JsonProperty("currentNumberOfTransactions")]
        public long CurrentNumberOfTransactions { get; set; }
    }
}
