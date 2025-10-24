using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using fiskaltrust.Middleware.SCU.BE.ZwarteDoos.Converters;
using fiskaltrust.Middleware.SCU.BE.ZwarteDoos.Models.Enums;
using fiskaltrust.Middleware.SCU.BE.ZwarteDoos.Models.Sale;

namespace fiskaltrust.Middleware.SCU.BE.ZwarteDoos.Models.Shared;

public class SignResult
{
    [JsonPropertyName("posId")]
    public required string PosId { get; set; }

    [JsonPropertyName("posFiscalTicketNo")]
    public required int PosFiscalTicketNo { get; set; }

    [JsonPropertyName("posDateTime")]
    [JsonConverter(typeof(Iso8601DateTimeConverter))]
    public required DateTime PosDateTime { get; set; }

    [JsonPropertyName("posSwVersion")]
    public required string PosSwVersion { get; set; }

    [JsonPropertyName("terminalId")]
    public required string TerminalId { get; set; }

    [JsonPropertyName("deviceId")]
    public required string DeviceId { get; set; }

    [JsonPropertyName("eventOperation")]
    public required EventOperation EventOperation { get; set; }

    [JsonPropertyName("fdmRef")]
    public required FdmReferenceInput FdmRef { get; set; }

    [JsonPropertyName("fdmSwVersion")]
    public required string FdmSwVersion { get; set; }

    [JsonPropertyName("digitalSignature")]
    public required string DigitalSignature { get; set; }

    [JsonPropertyName("shortSignature")]
    public string? ShortSignature { get; set; }

    [JsonPropertyName("verificationUrl")]
    public string? VerificationUrl { get; set; }

    [JsonPropertyName("vatCalc")]
    public List<VatCalcItem>? VatCalc { get; set; }

    [JsonPropertyName("bufferCapacityUsed")]
    public required decimal BufferCapacityUsed { get; set; }

    [JsonPropertyName("warnings")]
    public List<MessageItem>? Warnings { get; set; }

    [JsonPropertyName("informations")]
    public List<MessageItem>? Informations { get; set; }

    [JsonPropertyName("footer")]
    public required List<string> Footer { get; set; }
}
