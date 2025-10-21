﻿using System.Text.Json.Serialization;

namespace fiskaltrust.SAFT.CLI.SAFTSchemaPT10401;

public class PTUserObject
{
    [JsonPropertyName("UserId")]
    public string? UserId { get; set; }
    [JsonPropertyName("UserDisplayName")]
    public string? UserDisplayName { get; set; }
    [JsonPropertyName("UserEmail")]
    public string? UserEmail { get; set; }
}
