using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using fiskaltrust.Middleware.SCU.BE.ZwarteDoos.Converters;

namespace fiskaltrust.Middleware.SCU.BE.ZwarteDoos.Models.Report;

public class UserItemInput
{
    [JsonPropertyName("employeeId")]
    public required string EmployeeId { get; set; }

    [JsonPropertyName("totalAmount")]
    public required decimal TotalAmount { get; set; }

    [JsonPropertyName("firstPosDateTime")]
    [JsonConverter(typeof(Iso8601DateTimeConverter))]
    public required DateTime FirstPosDateTime { get; set; }

    [JsonPropertyName("lastPosDateTime")]
    [JsonConverter(typeof(Iso8601DateTimeConverter))]
    public required DateTime LastPosDateTime { get; set; }

    [JsonPropertyName("socialEvents")]
    public required List<InOutItemInput> SocialEvents { get; set; }

    [JsonPropertyName("payments")]
    public required List<PaymentTotalInput> Payments { get; set; }
}