using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace fiskaltrust.Middleware.SCU.GR.MyData;

public class InvoiceRowTypeOverride
{
    [JsonPropertyName("recType")]
    public int? RecType { get; set; }

    [JsonPropertyName("fuelCode")]
    public int? FuelCode { get; set; }

    [JsonPropertyName("invoiceDetailType")]
    public int? InvoiceDetailType { get; set; }

    [JsonPropertyName("dienergia")]
    public ShipTypeOverride? Dienergia { get; set; }

    [JsonPropertyName("discountOption")]
    public bool? DiscountOption { get; set; }

    [JsonPropertyName("lineComments")]
    public string? LineComments { get; set; }

    [JsonPropertyName("incomeClassification")]
    public List<IncomeClassificationOverride>? IncomeClassification { get; set; }

    [JsonPropertyName("expensesClassification")]
    public List<ExpensesClassificationOverride>? ExpensesClassification { get; set; }

    [JsonPropertyName("quantity15")]
    public decimal? Quantity15 { get; set; }

    [JsonPropertyName("taricNo")]
    public string? TaricNo { get; set; }

    [JsonPropertyName("itemCode")]
    public string? ItemCode { get; set; }

    [JsonPropertyName("otherMeasurementUnitQuantity")]
    public int? OtherMeasurementUnitQuantity { get; set; }

    [JsonPropertyName("otherMeasurementUnitTitle")]
    public string? OtherMeasurementUnitTitle { get; set; }

    [JsonPropertyName("notVAT195")]
    public bool? NotVAT195 { get; set; }
}

public class IncomeClassificationOverride
{
    [JsonPropertyName("classificationType")]
    public string? ClassificationType { get; set; }

    [JsonPropertyName("classificationCategory")]
    public string? ClassificationCategory { get; set; }
}

public class ExpensesClassificationOverride
{
    [JsonPropertyName("classificationType")]
    public string? ClassificationType { get; set; }

    [JsonPropertyName("classificationCategory")]
    public string? ClassificationCategory { get; set; }

    [JsonPropertyName("amount")]
    public decimal? Amount { get; set; }

    [JsonPropertyName("vatAmount")]
    public decimal? VatAmount { get; set; }

    [JsonPropertyName("vatCategory")]
    public int? VatCategory { get; set; }

    [JsonPropertyName("vatExemptionCategory")]
    public int? VatExemptionCategory { get; set; }
}
