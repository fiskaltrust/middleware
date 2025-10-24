using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using fiskaltrust.Middleware.SCU.BE.ZwarteDoos.Models.Shared;

namespace fiskaltrust.Middleware.SCU.BE.ZwarteDoos.Models.Report;

public class ReportTurnoverXInput : BaseInputData
{
    [JsonPropertyName("posDevices")]
    public required List<PosDeviceInput> PosDevices { get; set; }

    [JsonPropertyName("fdmDevices")]
    public required List<FdmDeviceInput> FdmDevices { get; set; }

    [JsonPropertyName("turnover")]
    public required TurnoverInput Turnover { get; set; }
}
