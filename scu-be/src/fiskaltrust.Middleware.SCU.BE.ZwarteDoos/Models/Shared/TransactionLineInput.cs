using System.Collections.Generic;
using System.Text.Json.Serialization;
using fiskaltrust.Middleware.SCU.BE.ZwarteDoos.Models.Enums;

namespace fiskaltrust.Middleware.SCU.BE.ZwarteDoos.Models.Shared;

public class TransactionLineInput
{
    [JsonPropertyName("lineType")]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public required TransactionLineType LineType { get; set; }

    [JsonPropertyName("mainProduct")]
    public required ProductInput MainProduct { get; set; }

    [JsonPropertyName("subProducts")]
    public List<ProductInput>? SubProducts { get; set; }

    [JsonPropertyName("costCenter")]
    public CostCenterInput? CostCenter { get; set; }

    [JsonPropertyName("lineTotal")]
    public required decimal LineTotal { get; set; }
}
