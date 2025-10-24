using System;
using System.Text.Json.Serialization;
using fiskaltrust.Middleware.SCU.BE.ZwarteDoos.Converters;

namespace fiskaltrust.Middleware.SCU.BE.ZwarteDoos.Models.Report;

public class PosDeviceInput
{
    [JsonPropertyName("posId")]
    public required string PosId { get; set; }

    [JsonPropertyName("terminalId")]
    public required string TerminalId { get; set; }

    [JsonPropertyName("firstPosDateTime")]
    [JsonConverter(typeof(Iso8601DateTimeConverter))]
    public required DateTime FirstPosDateTime { get; set; }

    [JsonPropertyName("lastPosDateTime")]
    [JsonConverter(typeof(Iso8601DateTimeConverter))]
    public required DateTime LastPosDateTime { get; set; }
}