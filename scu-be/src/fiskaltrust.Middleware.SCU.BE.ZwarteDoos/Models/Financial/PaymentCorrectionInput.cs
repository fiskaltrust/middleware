using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using fiskaltrust.Middleware.SCU.BE.ZwarteDoos.Converters;
using fiskaltrust.Middleware.SCU.BE.ZwarteDoos.Models.Sale;
using fiskaltrust.Middleware.SCU.BE.ZwarteDoos.Models.Shared;

namespace fiskaltrust.Middleware.SCU.BE.ZwarteDoos.Models.Financial;

public class PaymentCorrectionInput : BaseInputData
{
    [JsonPropertyName("financials")]
    public required List<PaymentLineInput> Financials { get; set; }

    [JsonPropertyName("fdmRef")]
    public FdmReferenceInput? FdmRef { get; set; }
}
