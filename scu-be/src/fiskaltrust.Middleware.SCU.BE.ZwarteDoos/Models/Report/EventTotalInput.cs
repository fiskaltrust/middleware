using System.Text.Json.Serialization;
using fiskaltrust.Middleware.SCU.BE.ZwarteDoos.Models.Enums;

namespace fiskaltrust.Middleware.SCU.BE.ZwarteDoos.Models.Report;

public class EventTotalInput
{
    [JsonPropertyName("eventLabel")]
    public required EventLabel EventLabel { get; set; }

    [JsonPropertyName("ticketCount")]
    public required int TicketCount { get; set; }

    [JsonPropertyName("amount")]
    public required decimal Amount { get; set; }
}
