using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using fiskaltrust.Middleware.SCU.BE.ZwarteDoos.Converters;
using fiskaltrust.Middleware.SCU.BE.ZwarteDoos.Models.Shared;

namespace fiskaltrust.Middleware.SCU.BE.ZwarteDoos.Models.Sale;

public class SaleInput : BaseInputData
{
    /// <summary>
    /// When performing a complete refund of a previous transaction, that transaction must be referenced. The reference must be an exact copy of the one received from the fiscal data module for the earlier transaction.
    /// </summary>
    [JsonPropertyName("fdmRef")]
    public FdmReferenceInput? FdmRef { get; set; }

    /// <summary>
    /// The cost center the transaction is booked on. Sales events resulting from an earlier signed order must contain the cost center of that order.
    /// </summary>
    [JsonPropertyName("costCenter")]
    public CostCenterInput? CostCenter { get; set; }

    /// <summary>
    /// Contains the records for the deliveries of goods and/or services as registered by the system.
    /// </summary>
    [JsonPropertyName("transaction")]
    public TransactionInput? Transaction { get; set; }

    /// <summary>
    ///  The payments that were made.
    /// </summary>
    [JsonPropertyName("financials")]
    public required List<PaymentLineInput> Financials { get; set; }   
}
