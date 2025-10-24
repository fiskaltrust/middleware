using System.Text.Json.Serialization;

namespace fiskaltrust.Middleware.SCU.BE.ZwarteDoos.Models.Enums;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum PriceChangeScope
{
    LINE,
    EVENT
}
