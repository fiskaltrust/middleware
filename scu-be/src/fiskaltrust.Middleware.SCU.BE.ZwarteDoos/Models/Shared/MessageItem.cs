using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using fiskaltrust.Middleware.SCU.BE.ZwarteDoos.GraphQL;

namespace fiskaltrust.Middleware.SCU.BE.ZwarteDoos.Models.Shared;

public class MessageItem
{
    [JsonPropertyName("message")]
    public required string Message { get; set; }

    [JsonPropertyName("locations")]
    public List<LocationItem>? Locations { get; set; }

    [JsonPropertyName("extensions")]
    public required ExtensionItem Extensions { get; set; }
}
