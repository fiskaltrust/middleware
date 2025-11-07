using System.Text.Json.Serialization;

namespace fiskaltrust.Middleware.SCU.BE.ZwarteDoos.Models.Enums;

/// <summary>
/// The reason negative quantities were booked.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum NegQuantityReason
{
    REFUND,
    CORRECTION,
    WASTE,
    DAMAGE
}
