using System;
using System.Text.Json.Serialization;
using fiskaltrust.Middleware.SCU.BE.ZwarteDoos.Converters;
using fiskaltrust.Middleware.SCU.BE.ZwarteDoos.Models.Enums;

namespace fiskaltrust.Middleware.SCU.BE.ZwarteDoos.Models.Sale;

public class FdmReferenceInput
{
    [JsonPropertyName("fdmId")]
    public required string FdmId { get; set; }

    [JsonPropertyName("fdmDateTime")]
    [JsonConverter(typeof(Iso8601DateTimeSecondsConverter))]
    public required DateTime FdmDateTime { get; set; }

    [JsonPropertyName("eventLabel")]
    public required EventLabel EventLabel { get; set; }

    [JsonPropertyName("eventCounter")]
    public required int EventCounter { get; set; }

    [JsonPropertyName("totalCounter")]
    public required int TotalCounter { get; set; }
}