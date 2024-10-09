using System;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace fiskaltrust.Api.POS.Models.ifPOS.v2;

public class ChargeItem
{
    [JsonPropertyName("ftChargeItemId")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    [DataMember(EmitDefaultValue = false, IsRequired = false)]
    public Guid? ftChargeItemId { get; set; }

    [JsonPropertyName("Quantity")]
    [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
    [DataMember(EmitDefaultValue = true, IsRequired = true)]
    public required decimal Quantity { get; set; } = 1m;

    [JsonPropertyName("Description")]
    [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
    [DataMember(EmitDefaultValue = true, IsRequired = true)]
    public required string Description { get; set; }

    [JsonPropertyName("Amount")]
    [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
    [DataMember(EmitDefaultValue = true, IsRequired = true)]
    public required decimal Amount { get; set; } = 0;

    [JsonPropertyName("VATRate")]
    [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
    [DataMember(EmitDefaultValue = true, IsRequired = true)]
    public required decimal VATRate { get; set; } = 0;

    [JsonPropertyName("ftChargeItemCase")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    [DataMember(EmitDefaultValue = false, IsRequired = false)]
    public long ftChargeItemCase { get; set; } = 0;

    [JsonPropertyName("ftChargeItemCaseData")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    [DataMember(EmitDefaultValue = false, IsRequired = false)]
    public object? ftChargeItemCaseData { get; set; }

    [JsonPropertyName("VATAmount")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    [DataMember(EmitDefaultValue = false, IsRequired = false)]
    public decimal? VATAmount { get; set; }

    [JsonPropertyName("Moment")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    [DataMember(EmitDefaultValue = false, IsRequired = false)]
    public DateTime? Moment { get; set; }

    [JsonPropertyName("Position")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    [DataMember(EmitDefaultValue = false, IsRequired = false)]
    public decimal Position { get; set; } = 0;

    [JsonPropertyName("AccountNumber")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    [DataMember(EmitDefaultValue = false, IsRequired = false)]
    public string? AccountNumber { get; set; }

    [JsonPropertyName("CostCenter")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    [DataMember(EmitDefaultValue = false, IsRequired = false)]
    public string? CostCenter { get; set; }

    [JsonPropertyName("ProductGroup")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    [DataMember(EmitDefaultValue = false, IsRequired = false)]
    public string? ProductGroup { get; set; }

    [JsonPropertyName("ProductNumber")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    [DataMember(EmitDefaultValue = false, IsRequired = false)]
    public string? ProductNumber { get; set; }

    [JsonPropertyName("ProductBarcode")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    [DataMember(EmitDefaultValue = false, IsRequired = false)]
    public string? ProductBarcode { get; set; }

    [JsonPropertyName("Unit")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    [DataMember(EmitDefaultValue = false, IsRequired = false)]
    public string? Unit { get; set; }

    [JsonPropertyName("UnitQuantity")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    [DataMember(EmitDefaultValue = false, IsRequired = false)]
    public decimal? UnitQuantity { get; set; }

    [JsonPropertyName("UnitPrice")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    [DataMember(EmitDefaultValue = false, IsRequired = false)]
    public decimal? UnitPrice { get; set; }

    [JsonPropertyName("Currency")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    [DataMember(Order = 170, EmitDefaultValue = false, IsRequired = false)]
    public Currency Currency { get; set; }

    [JsonPropertyName("DecimalPrecisionMultiplier")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    [DataMember(Order = 180, EmitDefaultValue = false, IsRequired = false)]
    public int DecimalPrecisionMultiplierSerialization
    {
        get => DecimalPrecisionMultiplier == 1 ? 0 : DecimalPrecisionMultiplier;
        set => DecimalPrecisionMultiplier = value == 0 ? 1 : value;
    }

    [JsonIgnore]
    public int DecimalPrecisionMultiplier { get; set; } = 1;
}
