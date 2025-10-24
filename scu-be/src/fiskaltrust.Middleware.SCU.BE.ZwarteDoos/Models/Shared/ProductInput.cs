using System.Collections.Generic;
using System.Text.Json.Serialization;
using fiskaltrust.Middleware.SCU.BE.ZwarteDoos.Models.Enums;
using fiskaltrust.Middleware.SCU.BE.ZwarteDoos.Models.Report;

namespace fiskaltrust.Middleware.SCU.BE.ZwarteDoos.Models.Shared;

public class ProductInput
{
    [JsonPropertyName("gtin")]
    public string? Gtin { get; set; }

    [JsonPropertyName("productId")]
    public required string ProductId { get; set; }

    [JsonPropertyName("productName")]
    public required string ProductName { get; set; }

    [JsonPropertyName("departmentId")]
    public required string DepartmentId { get; set; }

    [JsonPropertyName("departmentName")]
    public required string DepartmentName { get; set; }

    [JsonPropertyName("quantity")]
    public required decimal Quantity { get; set; }

    [JsonPropertyName("quantityType")]
    public required QuantityType QuantityType { get; set; }

    [JsonPropertyName("negQuantityReason")]
    public string? NegQuantityReason { get; set; }

    [JsonPropertyName("unitPrice")]
    public required decimal UnitPrice { get; set; }

    [JsonPropertyName("vats")]
    public required List<VatInput> Vats { get; set; } = new();
}

public class VatInput
{
    [JsonPropertyName("label")]
    public required VatLabel Label { get; set; }

    [JsonPropertyName("price")]
    public required decimal Price { get; set; }

    [JsonPropertyName("priceChanges")]
    public required List<PriceChangeInput> PriceChanges { get; set; }

}
