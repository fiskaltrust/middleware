﻿using System;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace fiskaltrust.Api.POS.Models.ifPOS.v2;

public class SignatureItem
{
    [JsonPropertyName("ftSignatureItemId")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    [DataMember(EmitDefaultValue = false, IsRequired = false)]
    public Guid? ftSignatureItemId { get; set; }

    [JsonPropertyName("ftSignatureFormat")]
    [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
    [DataMember(EmitDefaultValue = true, IsRequired = true)]
    public required long ftSignatureFormat { get; set; }

    [JsonPropertyName("ftSignatureType")]
    [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
    [DataMember(EmitDefaultValue = true, IsRequired = true)]
    public required long ftSignatureType { get; set; }

    [JsonPropertyName("Caption")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    [DataMember(EmitDefaultValue = false, IsRequired = false)]
    public string? Caption { get; set; }

    [JsonPropertyName("Data")]
    [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
    [DataMember(EmitDefaultValue = true, IsRequired = true)]
    public required string Data { get; set; }
}