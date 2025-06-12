using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.Middleware.SCU.ES.TicketBAI.Models;

namespace fiskaltrust.Middleware.SCU.ES.TicketBAI;

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