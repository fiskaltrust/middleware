using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace fiskaltrust.Middleware.SCU.BE.ZwarteDoos.Models.Shared;

public class TransactionInput
{
    [JsonPropertyName("transactionLines")]
    public required List<TransactionLineInput> TransactionLines { get; set; } = [];

    [JsonPropertyName("transactionTotal")]
    public required decimal TransactionTotal { get; set; }
}
