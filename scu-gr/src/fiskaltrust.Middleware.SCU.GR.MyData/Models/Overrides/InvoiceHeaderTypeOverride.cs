using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace fiskaltrust.Middleware.SCU.GR.MyData;

public class InvoiceHeaderTypeOverride
{
    [JsonPropertyName("invoiceType")]
    public string? InvoiceType { get; set; }

    [JsonPropertyName("vatPaymentSuspension")]
    public bool? VatPaymentSuspension { get; set; }

    [JsonPropertyName("selfPricing")]
    public bool? SelfPricing { get; set; }

    [JsonPropertyName("dispatchDate")]
    public DateTime? DispatchDate { get; set; }

    [JsonPropertyName("dispatchTime")]
    public DateTime? DispatchTime { get; set; }

    [JsonPropertyName("vehicleNumber")]
    public string? VehicleNumber { get; set; }

    [JsonPropertyName("movePurpose")]
    public int? MovePurpose { get; set; }

    [JsonPropertyName("fuelInvoice")]
    public bool? FuelInvoice { get; set; }

    [JsonPropertyName("specialInvoiceCategory")]
    public int? SpecialInvoiceCategory { get; set; }

    [JsonPropertyName("invoiceVariationType")]
    public int? InvoiceVariationType { get; set; }

    [JsonPropertyName("otherCorrelatedEntities")]
    public List<EntityTypeOverride>? OtherCorrelatedEntities { get; set; }

    [JsonPropertyName("otherDeliveryNoteHeader")]
    public OtherDeliveryNoteHeaderTypeOverride? OtherDeliveryNoteHeader { get; set; }

    [JsonPropertyName("otherMovePurposeTitle")]
    public string? OtherMovePurposeTitle { get; set; }

    [JsonPropertyName("exchangeRate")]
    public decimal? ExchangeRate { get; set; }

    [JsonPropertyName("thirdPartyCollection")]
    public bool? ThirdPartyCollection { get; set; }

    [JsonPropertyName("totalCancelDeliveryOrders")]
    public bool? TotalCancelDeliveryOrders { get; set; }

    [JsonPropertyName("reverseDeliveryNote")]
    public bool? ReverseDeliveryNote { get; set; }

    [JsonPropertyName("reverseDeliveryNotePurpose")]
    public int? ReverseDeliveryNotePurpose { get; set; }

    [JsonPropertyName("correlatedInvoices")]
    public long[]? CorrelatedInvoices { get; set; }
}
