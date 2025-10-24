using System.Text.Json.Serialization;
using fiskaltrust.Middleware.SCU.BE.ZwarteDoos.Models.Enums;

namespace fiskaltrust.Middleware.SCU.BE.ZwarteDoos.Models.Report;

/// <summary>
/// Contains totals of negative quantities booked for the specified reason during the report period.
/// </summary>
public class NegQuantityTotalInput
{
    [JsonPropertyName("negQuantityReason")]
    public required NegQuantityReason NegQuantityReason { get; set; }

    [JsonPropertyName("negQuantityCount")]
    public required int NegQuantityCount { get; set; }

    [JsonPropertyName("ticketCount")]
    public required int TicketCount { get; set; }

    [JsonPropertyName("amount")]
    public required decimal Amount { get; set; }
}
