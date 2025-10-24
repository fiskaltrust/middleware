using System.Text.Json.Serialization;

namespace fiskaltrust.Middleware.SCU.BE.ZwarteDoos.Models.Enums;

/// <summary>
/// The event label the totals apply to.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum EventLabel
{
    SALES,
    REFUNDS,
    TRAINING
}
