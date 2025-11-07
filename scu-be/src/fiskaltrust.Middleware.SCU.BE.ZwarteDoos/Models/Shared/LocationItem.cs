using System.Text.Json.Serialization;

namespace fiskaltrust.Middleware.SCU.BE.ZwarteDoos.Models.Shared;

public class LocationItem
{
    [JsonPropertyName("line")]
    public int Line { get; set; }


    [JsonPropertyName("column")]
    public int Column { get; set; }
}