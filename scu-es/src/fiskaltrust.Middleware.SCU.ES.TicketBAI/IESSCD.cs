using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using fiskaltrust.ifPOS.v1;

namespace fiskaltrust.Middleware.SCU.ES.TicketBAI;

public interface IESSSCD
{
    Task<SubmitResponse> SubmitInvoiceAsync(SubmitInvoiceRequest request);

    string GetRawXml(SubmitInvoiceRequest request);
}

public class SubmitInvoiceRequest
{
    public DateTime InvoiceMoment { get; set; }
    public string Series { get; set; } = null!;
    public string InvoiceNumber { get; set; } = null!;
}

public class SubmitResponse
{
    public string? RequestContent { get; set; }
    public string? ResponseContent { get; set; }
    public bool Succeeded { get; set; }
    public Uri? QrCode { get; set; }
    public string? ShortSignatureValue { get; set; }
    public string? ExpeditionDate { get; set; }
    public string? IssuerVatId { get; set; }
    public string? Identifier { get; set; }
    public string? Explanation { get; set; }
    public string? Description { get; set; }
    public string? ErrorCode { get; set; }
}