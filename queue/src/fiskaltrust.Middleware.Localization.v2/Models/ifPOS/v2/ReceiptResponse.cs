using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;
using fiskaltrust.Middleware.Localization.v2.Models.ifPOS.v2.Cases;

namespace fiskaltrust.Api.POS.Models.ifPOS.v2;

public class ReceiptResponse
{
    [JsonPropertyName("ftQueueID")]
    [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
    [DataMember(EmitDefaultValue = true, IsRequired = true)]
    public required Guid ftQueueID { get; set; }

    [JsonPropertyName("ftQueueItemID")]
    [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
    [DataMember(EmitDefaultValue = true, IsRequired = true)]
    public required Guid ftQueueItemID { get; set; }

    [JsonPropertyName("ftQueueRow")]
    [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
    [DataMember(EmitDefaultValue = true, IsRequired = true)]
    public required long ftQueueRow { get; set; }

    [JsonPropertyName("ftCashBoxIdentification")]
    [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
    [DataMember(EmitDefaultValue = true, IsRequired = true)]
    public required string ftCashBoxIdentification { get; set; }

    [JsonPropertyName("ftCashBoxID")]
    [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
    [DataMember(EmitDefaultValue = true, IsRequired = true)]
    public Guid? ftCashBoxID { get; set; }

    [JsonPropertyName("cbTerminalID")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    [DataMember(EmitDefaultValue = true, IsRequired = true)]
    public string? cbTerminalID { get; set; }

    [JsonPropertyName("cbReceiptReference")]
    [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
    [DataMember(EmitDefaultValue = true, IsRequired = true)]
    public string? cbReceiptReference { get; set; }

    [JsonPropertyName("ftReceiptIdentification")]
    [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
    [DataMember(EmitDefaultValue = true, IsRequired = true)]
    public required string ftReceiptIdentification { get; set; }

    [JsonPropertyName("ftReceiptMoment")]
    [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
    [DataMember(EmitDefaultValue = true, IsRequired = true)]
    public required DateTime ftReceiptMoment { get; set; }

    [JsonPropertyName("ftReceiptHeader")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    [DataMember(EmitDefaultValue = true, IsRequired = true)]
    public List<string>? ftReceiptHeader { get; set; }

    [JsonPropertyName("ftChargeItems")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    [DataMember(EmitDefaultValue = true, IsRequired = true)]
    public List<ChargeItem>? ftChargeItems { get; set; }

    [JsonPropertyName("ftChargeLines")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    [DataMember(EmitDefaultValue = true, IsRequired = true)]
    public List<string>? ftChargeLines { get; set; }

    [JsonPropertyName("ftPayItems")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    [DataMember(EmitDefaultValue = true, IsRequired = true)]
    public List<PayItem>? ftPayItems { get; set; }

    [JsonPropertyName("ftPayLines")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    [DataMember(EmitDefaultValue = true, IsRequired = true)]
    public List<string>? ftPayLines { get; set; }

    [JsonPropertyName("ftSignatures")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    [DataMember(EmitDefaultValue = true, IsRequired = true)]
    public List<SignatureItem> ftSignatures { get; set; } = [];

    [JsonPropertyName("ftReceiptFooter")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    [DataMember(EmitDefaultValue = true, IsRequired = true)]
    public List<string>? ftReceiptFooter { get; set; }

    [JsonPropertyName("ftState")]
    [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
    [DataMember(EmitDefaultValue = true, IsRequired = true)]
    public required State ftState { get; set; }

    [JsonPropertyName("ftStateData")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    [DataMember(EmitDefaultValue = true, IsRequired = true)]
    public object? ftStateData { get; set; }
}
