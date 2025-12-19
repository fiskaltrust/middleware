using System.Text.Json.Serialization;

namespace fiskaltrust.Middleware.SCU.BE.ZwarteDoos.Models.Enums;

/// <summary>
/// The type of the price change. Usage is limited to type PUBLIC.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum PriceChangeType
{
    PUBLIC
}
