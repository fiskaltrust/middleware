using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using fiskaltrust.Middleware.SCU.BE.ZwarteDoos.Models.Shared;

namespace fiskaltrust.Middleware.SCU.BE.ZwarteDoos.Models.Report;

public class ReportUserZInput : BaseInputData
{
    [JsonPropertyName("reportNo")]
    public required int ReportNo { get; set; }

    [JsonPropertyName("reportBookingDate")]
    public required string ReportBookingDate { get; set; }

    [JsonPropertyName("posDevices")]
    public required List<PosDeviceInput> PosDevices { get; set; }

    [JsonPropertyName("fdmDevices")]
    public required List<FdmDeviceInput> FdmDevices { get; set; }

    [JsonPropertyName("users")]
    public required List<UserItemInput> Users { get; set; }
}
