using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace fiskaltrust.Api.POS.Models.ifPOS.v2;

public class ReceiptRequest
{
    [JsonPropertyName("cbTerminalID")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    [DataMember(Order = 10, EmitDefaultValue = false, IsRequired = false)]
    public string? cbTerminalID { get; set; }

    [JsonPropertyName("cbReceiptReference")]
    [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
    [DataMember(Order = 20, EmitDefaultValue = true, IsRequired = true)]
    public string? cbReceiptReference { get; set; }

    [JsonPropertyName("cbReceiptMoment")]
    [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
    [DataMember(Order = 30, EmitDefaultValue = true, IsRequired = true)]
    public DateTime cbReceiptMoment { get; set; }

    [JsonPropertyName("cbChargeItems")]
    [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
    [DataMember(Order = 40, EmitDefaultValue = true, IsRequired = true)]
    public List<ChargeItem> cbChargeItems { get; set; } = [];

    [JsonPropertyName("cbPayItems")]
    [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
    [DataMember(Order = 50, EmitDefaultValue = true, IsRequired = true)]
    public List<PayItem> cbPayItems { get; set; } = [];

    [JsonPropertyName("ftCashBoxID")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    [DataMember(Order = 60, EmitDefaultValue = false, IsRequired = false)]
    public Guid? ftCashBoxID { get; set; }

    [JsonPropertyName("ftPosSystemId")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    [DataMember(Order = 70, EmitDefaultValue = false, IsRequired = false)]
    public Guid? ftPosSystemId { get; set; }

    [JsonPropertyName("ftReceiptCase")]
    [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
    [DataMember(Order = 80, EmitDefaultValue = false, IsRequired = false)]
    public long ftReceiptCase { get; set; } = 0;

    [JsonPropertyName("ftReceiptCaseData")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    [DataMember(Order = 90, EmitDefaultValue = false, IsRequired = false)]
    public object? ftReceiptCaseData { get; set; }

    [JsonPropertyName("ftQueueID")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    [DataMember(Order = 100, EmitDefaultValue = false, IsRequired = false)]
    public Guid? ftQueueID { get; set; }

    [JsonPropertyName("cbPreviousReceiptReference")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    [DataMember(Order = 110, EmitDefaultValue = false, IsRequired = false)]
    public string? cbPreviousReceiptReference { get; set; }

    [JsonPropertyName("cbReceiptAmount")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    [DataMember(Order = 120, EmitDefaultValue = false, IsRequired = false)]
    public decimal? cbReceiptAmount { get; set; }

    [JsonPropertyName("cbUser")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    [DataMember(Order = 130, EmitDefaultValue = false, IsRequired = false)]
    public object? cbUser { get; set; }

    [JsonPropertyName("cbArea")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    [DataMember(Order = 140, EmitDefaultValue = false, IsRequired = false)]
    public object? cbArea { get; set; }

    [JsonPropertyName("cbCustomer")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    [DataMember(Order = 150, EmitDefaultValue = false, IsRequired = false)]
    public object? cbCustomer { get; set; }

    [JsonPropertyName("cbSettlement")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    [DataMember(Order = 160, EmitDefaultValue = false, IsRequired = false)]
    public object? cbSettlement { get; set; }

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
