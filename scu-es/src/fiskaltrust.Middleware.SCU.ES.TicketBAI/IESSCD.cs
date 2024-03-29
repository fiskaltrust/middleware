﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using fiskaltrust.ifPOS.v1;

namespace fiskaltrust.Middleware.SCU.ES.TicketBAI;

public interface IESSSCD
{
    Task<SubmitResponse> SubmitInvoiceAsync(SubmitInvoiceRequest request);
}

public class SubmitInvoiceRequest
{
    public string ftCashBoxIdentification { get; set; } = null!;

    public DateTime InvoiceMoment { get; set; }
    public string Series { get; set; } = null!;
    public string InvoiceNumber { get; set; } = null!;

    public string? LastInvoiceNumber { get; set; }
    public DateTime? LastInvoiceMoment { get; set; }
    public string? LastInvoiceSignature { get; set; }

    public List<InvoiceLine> InvoiceLine { get; set; } = new List<InvoiceLine>();
}

public class InvoiceLine
{
    public string Description { get; set; } = null!;
    public decimal Quantity { get; set; }
    public decimal Amount { get; set; }
    public decimal VATAmount { get; set; }
    public decimal VATRate { get; set; }
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
    public List<(string code, string message)> ResultMessages { get; set; } = new List<(string code, string message)>();
}