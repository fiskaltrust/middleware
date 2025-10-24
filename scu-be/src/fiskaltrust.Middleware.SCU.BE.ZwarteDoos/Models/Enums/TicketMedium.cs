using System.Text.Json.Serialization;

namespace fiskaltrust.Middleware.SCU.BE.ZwarteDoos.Models.Enums;

/// <summary>
/// Specifies the method of ticket delivery used by the POS system for the transaction.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum TicketMedium
{
    ELECTRONIC,
    PAPER
}
