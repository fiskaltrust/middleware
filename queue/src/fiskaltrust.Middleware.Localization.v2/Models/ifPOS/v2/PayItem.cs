using System;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;
using fiskaltrust.Middleware.Localization.v2.Models.ifPOS.v2.Cases;

namespace fiskaltrust.Api.POS.Models.ifPOS.v2;

public class PayItem
{
    [JsonPropertyName("ftPayItemId")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    [DataMember(EmitDefaultValue = false, IsRequired = false)]
    public Guid? ftPayItemId { get; set; }

    [JsonPropertyName("Quantity")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    [DataMember(EmitDefaultValue = true, IsRequired = true)]
    public decimal? QuantitySerialization
    {
        get => Quantity == 1 ? null : Quantity;
        set => Quantity = value ?? 1;
    }

    [JsonIgnore]
    public decimal Quantity { get; set; } = 1;

    [JsonPropertyName("Description")]
    [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
    [DataMember(EmitDefaultValue = true, IsRequired = true)]
    public string? Description { get; set; }

    [JsonPropertyName("Amount")]
    [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
    [DataMember(EmitDefaultValue = true, IsRequired = true)]
    public decimal Amount { get; set; }

    [JsonPropertyName("ftPayItemCase")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    [DataMember(EmitDefaultValue = false, IsRequired = false)]
    public PayItemCase ftPayItemCase { get; set; } = 0x0;

    [JsonPropertyName("ftPayItemCaseData")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    [DataMember(EmitDefaultValue = false, IsRequired = false)]
    public object? ftPayItemCaseData { get; set; }

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

    [JsonPropertyName("MoneyGroup")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    [DataMember(EmitDefaultValue = false, IsRequired = false)]
    public string? MoneyGroup { get; set; }

    [JsonPropertyName("MoneyNumber")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    [DataMember(EmitDefaultValue = false, IsRequired = false)]
    public string? MoneyNumber { get; set; }

    [JsonPropertyName("MoneyBarcode")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    [DataMember(EmitDefaultValue = false, IsRequired = false)]
    public string? MoneyBarcode { get; set; }

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
