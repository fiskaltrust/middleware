using System;
using System.Text.Json.Serialization;
using fiskaltrust.Middleware.SCU.BE.ZwarteDoos.Converters;

namespace fiskaltrust.Middleware.SCU.BE.ZwarteDoos.Models.Report;

public class FdmDeviceInput
{
    [JsonPropertyName("fdmId")]
    public required string FdmId { get; set; }

    [JsonPropertyName("firstFdmDateTime")]
    [JsonConverter(typeof(Iso8601DateTimeConverter))]
    public required DateTime FirstFdmDateTime { get; set; }

    [JsonPropertyName("firstTotalCounter")]
    public required int FirstTotalCounter { get; set; }

    [JsonPropertyName("lastFdmDateTime")]
    [JsonConverter(typeof(Iso8601DateTimeConverter))]
    public required DateTime LastFdmDateTime { get; set; }

    [JsonPropertyName("lastTotalCounter")]
    public required int LastTotalCounter { get; set; }
}
