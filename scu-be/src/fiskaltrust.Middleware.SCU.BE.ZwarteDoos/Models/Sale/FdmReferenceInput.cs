using System;
using System.Text.Json.Serialization;

namespace fiskaltrust.Middleware.SCU.BE.ZwarteDoos.Models.Sale;

public class FdmReferenceInput
{
    [JsonPropertyName("fdmId")]
    public string? FdmId { get; set; }

    [JsonPropertyName("fdmDateTime")]
    public string? FdmDateTime { get; set; }

    [JsonPropertyName("eventLabel")]
    public string? EventLabel { get; set; }

    [JsonPropertyName("eventCounter")]
    public int? EventCounter { get; set; }

    [JsonPropertyName("totalCounter")]
    public int? TotalCounter { get; set; }
}