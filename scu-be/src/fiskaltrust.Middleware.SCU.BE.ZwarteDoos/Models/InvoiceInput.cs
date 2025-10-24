using System.Collections.Generic;
using System.Text.Json.Serialization;
using fiskaltrust.Middleware.SCU.BE.ZwarteDoos.Models.Shared;

namespace fiskaltrust.Middleware.SCU.BE.ZwarteDoos.Models;

/// <summary>
/// Input data for SignInvoice mutation - delivery of invoices based on earlier VAT receipts
/// </summary>
public class InvoiceInput
{
    [JsonPropertyName("language")]
    public string Language { get; set; } = "NL";

    [JsonPropertyName("vatNo")]
    public string? VatNo { get; set; }

    [JsonPropertyName("estNo")]
    public string? EstNo { get; set; }

    [JsonPropertyName("posId")]
    public string? PosId { get; set; }

    [JsonPropertyName("posFiscalTicketNo")]
    public int? PosFiscalTicketNo { get; set; }

    [JsonPropertyName("posDateTime")]
    public string? PosDateTime { get; set; }

    [JsonPropertyName("posSwVersion")]
    public string? PosSwVersion { get; set; }

    [JsonPropertyName("deviceId")]
    public string? DeviceId { get; set; }

    [JsonPropertyName("terminalId")]
    public string? TerminalId { get; set; }

    [JsonPropertyName("bookingPeriodId")]
    public string? BookingPeriodId { get; set; }

    [JsonPropertyName("bookingDate")]
    public string? BookingDate { get; set; }

    [JsonPropertyName("ticketMedium")]
    public string TicketMedium { get; set; } = "PAPER";

    [JsonPropertyName("employeeId")]
    public string? EmployeeId { get; set; }

    [JsonPropertyName("costCenter")]
    public CostCenterInput? CostCenter { get; set; }

    [JsonPropertyName("invoiceNumber")]
    public string? InvoiceNumber { get; set; }

    [JsonPropertyName("invoiceDate")]
    public string? InvoiceDate { get; set; }

    [JsonPropertyName("customerInfo")]
    public CustomerInfo? CustomerInfo { get; set; }

    [JsonPropertyName("relatedReceipts")]
    public List<RelatedReceipt>? RelatedReceipts { get; set; }

    [JsonPropertyName("transaction")]
    public TransactionInput? Transaction { get; set; }
}
