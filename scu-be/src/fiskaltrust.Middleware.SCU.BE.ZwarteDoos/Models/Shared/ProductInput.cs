using System.Collections.Generic;
using System.Text.Json.Serialization;
using fiskaltrust.Middleware.SCU.BE.ZwarteDoos.Models.Enums;
using fiskaltrust.Middleware.SCU.BE.ZwarteDoos.Models.Report;

namespace fiskaltrust.Middleware.SCU.BE.ZwarteDoos.Models.Shared;

public class ProductInput
{
    /// <summary>
    /// The Global Trade Item Number for the good or service. This field is optional. A maximum of 20 characters is allowed with a minimum of 1 character. Leading or trailing whitespace characters are not allowed in the field.
    /// </summary>
    [JsonPropertyName("gtin")]
    public string? Gtin { get; set; }

    /// <summary>
    /// The language independent and case sensitive id of the good or service. A maximum of 600 characters is allowed with a minimum of 1 character. Leading or trailing whitespace characters are not allowed in the field.
    /// This is not an identification of an individual sale if a good or service. All product lines involving the same good or service must share the same id.
    /// </summary>
    [JsonPropertyName("productId")]
    public required string ProductId { get; set; }

    /// <summary>
    /// The name for the good or service. A maximum of 600 characters is allowed with a minimum of 1 character. Leading or trailing whitespace characters are not allowed in the field.
    /// </summary>
    [JsonPropertyName("productName")]
    public required string ProductName { get; set; }

    /// <summary>
    /// The language independent and case sensitive id of the department that the good or service belongs to. A maximum of 600 characters is allowed with a minimum of 1 character. Leading or trailing whitespace characters are not allowed in the field.
    /// </summary>
    [JsonPropertyName("departmentId")]
    public required string DepartmentId { get; set; }

    /// <summary>
    /// The name of the department. A maximum of 600 characters is allowed with a minimum of 1 character. Leading or trailing whitespace characters are not allowed in the field.
    /// </summary>
    [JsonPropertyName("departmentName")]
    public required string DepartmentName { get; set; }

    /// <summary>
    /// The number of whole and/or partial units of the product that applies to the product line. This can be positive, negative, or zero.
    /// </summary>
    [JsonPropertyName("quantity")]
    public required decimal Quantity { get; set; }

    /// <summary>
    /// The unit of measure that applies to the quantity.
    /// </summary>
    [JsonPropertyName("quantityType")]
    public required QuantityType QuantityType { get; set; }

    /// <summary>
    /// When the quantity is a negative amount the reason has to be specified.
    /// </summary>
    [JsonPropertyName("negQuantityReason")]
    public string? NegQuantityReason { get; set; }

    /// <summary>
    /// The regular price, including VAT and before price changes, for a single unit of the product.
    /// </summary>
    [JsonPropertyName("unitPrice")]
    public required decimal UnitPrice { get; set; }

    /// <summary>
    /// Specifies the price part and price changes per VAT rate that make up the selling price for the product. This array must be empty for the main product when that main product is part of a composite product. For composite products only the vats arrays of the sub products are used.
    /// </summary>
    [JsonPropertyName("vats")]
    public required List<VatInput> Vats { get; set; } = new();
}

public class VatInput
{
    /// <summary>
    /// The label of the VAT rate that applies to the product.
    /// </summary>
    [JsonPropertyName("label")]
    public required VatLabel Label { get; set; }

    /// <summary>
    /// The part of the selling price of the product that is subject to the VAT rate specified with the label. The amount can be positive, negative, or zero. If a product is subject to multiple VAT rates the selling price is split accordingly between multiple VatInput objects.
    /// </summary>
    [JsonPropertyName("price")]
    public required decimal Price { get; set; }

    /// <summary>
    /// A list of price changes that applies to the product for the VAT rate specified with the label. The list can contain up to 99 PriceChangeInput objects.
    /// </summary>
    [JsonPropertyName("priceChanges")]
    public required List<PriceChangeInput> PriceChanges { get; set; }

}
