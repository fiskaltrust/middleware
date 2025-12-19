using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace fiskaltrust.Middleware.SCU.BE.ZwarteDoos.Models.Shared;

public class TransactionInput
{
    /// <summary>
    /// Lists the goods and services with their quantity, price and price changes, that make up the transaction.
    /// </summary>
    [JsonPropertyName("transactionLines")]
    public required List<TransactionLineInput> TransactionLines { get; set; } = [];

    /// <summary>
    /// The total calculated on the transaction lines (sum of lineTotal).
    /// </summary>
    [JsonPropertyName("transactionTotal")]
    public required decimal TransactionTotal { get; set; }
}
