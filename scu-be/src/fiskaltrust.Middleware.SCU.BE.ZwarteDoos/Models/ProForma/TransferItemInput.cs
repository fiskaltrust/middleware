using System.Text.Json.Serialization;
using fiskaltrust.Middleware.SCU.BE.ZwarteDoos.Models.Shared;

namespace fiskaltrust.Middleware.SCU.BE.ZwarteDoos.Models.ProForma;

public class TransferItemInput
{
    [JsonPropertyName("costCenter")]
    public required CostCenterInput CostCenter { get; set; }

    [JsonPropertyName("transaction")]
    public required TransactionInput Transaction { get; set; }
}