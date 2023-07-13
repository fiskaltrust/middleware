using System;
using System.Threading.Tasks;
using fiskaltrust.Middleware.SCU.ES.TicketBAI.Models;

namespace fiskaltrust.Middleware.SCU.ES.TicketBAI;

public interface IESSSCD
{
    Task<SubmitResponse> SubmitInvoiceAsync(TicketBaiRequest ticketBaiRequest);

    string GetRawXml(TicketBaiRequest ticketBaiRequest);
}

public class SubmitResponse
{
    public string? Content { get; set; }
    public bool Succeeded { get; set; }
    public Uri? QrCode { get; set; }
    public string? ShortSignatureValue { get; set; }
    public string? ExpeditionDate { get; set; }
    public string? IssuerVatId { get; set; }
    public string? Identifier { get; set; }
}