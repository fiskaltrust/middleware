using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using fiskaltrust.Middleware.SCU.BE.ZwarteDoos.Converters;
using fiskaltrust.Middleware.SCU.BE.ZwarteDoos.Models.Sale;
using fiskaltrust.Middleware.SCU.BE.ZwarteDoos.Models.Shared;

namespace fiskaltrust.Middleware.SCU.BE.ZwarteDoos.Models.Invoice;

public class InvoiceInput : BaseInputData
{
    [JsonPropertyName("invoiceNo")]
    public required string InvoiceNo { get; set; }

    [JsonPropertyName("customerVatNo")]
    public required string CustomerVatNo { get; set; }

    [JsonPropertyName("costCenter")]
    public CostCenterInput? CostCenter { get; set; }

    [JsonPropertyName("fdmRefs")]
    public required List<FdmReferenceInput> FdmRefs { get; set; }
}
