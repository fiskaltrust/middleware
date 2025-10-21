using System;
using System.Collections.Generic;

namespace fiskaltrust.Middleware.SCU.BE.ZwarteDoos.Models;

public class SignatureType
{
    public string Value { get; set; } = null!;
    public string Algorithm { get; set; } = null!;
}

public class ZwarteDoosConfiguration
{
    public string ServiceUrl { get; set; } = null!;
    public string ApiKey { get; set; } = null!;
    public string CompanyId { get; set; } = null!;
    public bool SandboxMode { get; set; } = true;
    public int TimeoutSeconds { get; set; } = 30;
}

public class ZwarteDoosInvoiceRequest
{
    public string CompanyId { get; set; } = null!;
    public string InvoiceNumber { get; set; } = null!;
    public DateTime InvoiceDate { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal VatAmount { get; set; }
    public List<ZwarteDoosInvoiceLine> Lines { get; set; } = new List<ZwarteDoosInvoiceLine>();
}

public class ZwarteDoosInvoiceLine
{
    public string Description { get; set; } = null!;
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal VatRate { get; set; }
    public decimal Amount { get; set; }
}

public class ZwarteDoosInvoiceResponse
{
    public bool Success { get; set; }
    public string? Signature { get; set; }
    public string? QrCode { get; set; }
    public string? TransactionId { get; set; }
    public List<string> Errors { get; set; } = new List<string>();
    public DateTime Timestamp { get; set; }
}