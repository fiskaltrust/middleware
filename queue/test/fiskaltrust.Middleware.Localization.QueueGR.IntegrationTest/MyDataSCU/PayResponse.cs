using System.Runtime.Serialization;
using System.Text.Json.Serialization;
using fiskaltrust.Api.POS.Models.ifPOS.v2;

namespace fiskaltrust.Middleware.Localization.QueueGR.IntegrationTest.MyDataSCU
{
    public class PayResponse
    {
        [JsonPropertyName("Protocol")]
        [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
        [DataMember(EmitDefaultValue = true, IsRequired = true)]
        public required string Protocol { get; set; }

        [JsonPropertyName("ftQueueID")]
        [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
        [DataMember(EmitDefaultValue = true, IsRequired = true)]
        public Guid ftQueueId { get; set; }

        [JsonPropertyName("ftPayItems")]
        [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
        [DataMember(EmitDefaultValue = true, IsRequired = true)]
        public required List<PayItem> ftPayItems { get; set; }
    }
}