using System.Collections.Generic;
using System.Text.Json.Serialization;
using fiskaltrust.Middleware.SCU.BE.ZwarteDoos.Models.Sale;
using fiskaltrust.Middleware.SCU.BE.ZwarteDoos.Models.Shared;

namespace fiskaltrust.Middleware.SCU.BE.ZwarteDoos.Models.ProForma;

public class PreBillInput : BaseInputData
{
    [JsonPropertyName("costCenter")]
    public CostCenterInput? CostCenter { get; set; }

    [JsonPropertyName("transaction")]
    public required TransactionInput Transaction { get; set; }

    [JsonPropertyName("financials")]
    public List<PaymentLineInput>? Financials { get; set; }
}
