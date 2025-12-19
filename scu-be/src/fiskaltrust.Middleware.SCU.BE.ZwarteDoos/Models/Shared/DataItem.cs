using System;
using System.Text.Json.Serialization;

namespace fiskaltrust.Middleware.SCU.BE.ZwarteDoos.Models.Shared;

public class DataItem
{
    [JsonPropertyName("name")]
    public required string Name { get; set; }
    [JsonPropertyName("value")]
    public required string Value { get; set; }
}