using System;
using System.Text.Json.Serialization;
using fiskaltrust.Middleware.SCU.BE.ZwarteDoos.Converters;
using fiskaltrust.Middleware.SCU.BE.ZwarteDoos.Models.Enums;
using fiskaltrust.Middleware.SCU.BE.ZwarteDoos.Models.Sale;

namespace fiskaltrust.Middleware.SCU.BE.ZwarteDoos.Models.Shared;

public class BaseInputData
{
    [JsonPropertyName("language")]
    public required Language Language { get; set; } = Language.NL;

    [JsonPropertyName("vatNo")]
    public required string VatNo { get; set; }

    [JsonPropertyName("estNo")]
    public required string EstNo { get; set; }

    [JsonPropertyName("posId")]
    public required string PosId { get; set; }

    [JsonPropertyName("posFiscalTicketNo")]
    public required long PosFiscalTicketNo { get; set; }

    [JsonPropertyName("posDateTime")]
    [JsonConverter(typeof(Iso8601DateTimeConverter))]
    public required DateTime PosDateTime { get; set; }

    [JsonPropertyName("posSwVersion")]
    public required string PosSwVersion { get; set; }

    [JsonPropertyName("terminalId")]
    public required string TerminalId { get; set; }

    [JsonPropertyName("deviceId")]
    public required string DeviceId { get; set; }

    [JsonPropertyName("bookingPeriodId")]
    public required Guid BookingPeriodId { get; set; }

    [JsonPropertyName("bookingDate")]
    public required DateOnly BookingDate { get; set; }

    [JsonPropertyName("ticketMedium")]
    public required TicketMedium TicketMedium { get; set; } 

    [JsonPropertyName("employeeId")]
    public required string EmployeeId { get; set; }
}
