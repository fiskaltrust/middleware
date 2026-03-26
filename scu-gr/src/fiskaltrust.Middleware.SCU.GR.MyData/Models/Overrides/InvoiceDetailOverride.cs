using System.Text.Json.Serialization;

namespace fiskaltrust.Middleware.SCU.GR.MyData;

public class InvoiceDetailOverride
{
    [JsonPropertyName("taricNo")]
    public string? TaricNo { get; set; }

    [JsonPropertyName("itemCode")]
    public string? ItemCode { get; set; }

    [JsonPropertyName("fuelCode")]
    public int? FuelCode { get; set; }

    [JsonPropertyName("lineComments")]
    public string? LineComments { get; set; }

    [JsonPropertyName("quantity15")]
    public decimal? Quantity15 { get; set; }

    [JsonPropertyName("otherMeasurementUnitQuantity")]
    public int? OtherMeasurementUnitQuantity { get; set; }

    [JsonPropertyName("otherMeasurementUnitTitle")]
    public string? OtherMeasurementUnitTitle { get; set; }

    [JsonPropertyName("notVAT195")]
    public bool? NotVAT195 { get; set; }

    [JsonPropertyName("dienergia")]
    public ShipOverride? Dienergia { get; set; }

    [JsonPropertyName("discountOption")]
    public bool? DiscountOption { get; set; }
}
