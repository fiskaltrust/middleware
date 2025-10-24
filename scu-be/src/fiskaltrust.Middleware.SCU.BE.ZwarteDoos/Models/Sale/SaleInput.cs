using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using fiskaltrust.Middleware.SCU.BE.ZwarteDoos.Converters;
using fiskaltrust.Middleware.SCU.BE.ZwarteDoos.Models.Shared;

namespace fiskaltrust.Middleware.SCU.BE.ZwarteDoos.Models.Sale;

public class SaleInput : BaseInputData
{
    [JsonPropertyName("fdmRef")]
    public FdmReferenceInput? FdmRef { get; set; }

    [JsonPropertyName("costCenter")]
    public CostCenterInput? CostCenter { get; set; }

    [JsonPropertyName("transaction")]
    public TransactionInput? Transaction { get; set; }

    [JsonPropertyName("financials")]
    public required List<PaymentLineInput> Financials { get; set; }   
}
