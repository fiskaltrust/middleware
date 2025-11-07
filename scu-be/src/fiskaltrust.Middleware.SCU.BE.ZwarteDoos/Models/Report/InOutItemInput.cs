using System;
using System.Text.Json.Serialization;
using fiskaltrust.Middleware.SCU.BE.ZwarteDoos.Converters;
using fiskaltrust.Middleware.SCU.BE.ZwarteDoos.Models.Enums;

namespace fiskaltrust.Middleware.SCU.BE.ZwarteDoos.Models.Report;

public class InOutItemInput
{
    [JsonPropertyName("posDateTime")]
    [JsonConverter(typeof(Iso8601DateTimeConverter))]
    public required DateTime PosDateTime { get; set; }

    [JsonPropertyName("inOut")]
    public required InOut InOut { get; set; }
}
