using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace fiskaltrust.Middleware.SCU.BE.ZwarteDoos.Models.Shared;

public class TransactionLineInput
{
    [JsonPropertyName("lineType")]
    public required string LineType { get; set; }

    [JsonPropertyName("mainProduct")]
    public required ProductInput MainProduct { get; set; }

    [JsonPropertyName("subProducts")]
    public List<ProductInput>? SubProducts { get; set; }

    [JsonPropertyName("costCenter")]
    public CostCenterInput? CostCenter { get; set; }

    [JsonPropertyName("lineTotal")]
    public required decimal LineTotal { get; set; }
}
