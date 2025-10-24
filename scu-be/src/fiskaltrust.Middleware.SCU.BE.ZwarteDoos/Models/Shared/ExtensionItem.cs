using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using fiskaltrust.Middleware.SCU.BE.ZwarteDoos.Models.Enums;

namespace fiskaltrust.Middleware.SCU.BE.ZwarteDoos.Models.Shared;

public class ExtensionItem
{
    [JsonPropertyName("category")]
    public required Category Category { get; set; }

    [JsonPropertyName("code")]
    // We keep this as string to make sure that we are not affected by changes in the responded values
    public required string Code { get; set; }

    [JsonPropertyName("data")]
    public List<DataItem>? Data { get; set; }

    [JsonPropertyName("showPos")]
    public required Display ShowPos { get; set; }
}