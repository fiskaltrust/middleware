using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.Middleware.SCU.ES.TicketBAI.Models;

namespace fiskaltrust.Middleware.SCU.ES.TicketBAI;

public class InvoiceLine
{
    public string Description { get; set; } = null!;
    public decimal Quantity { get; set; }
    public decimal Amount { get; set; }
    public decimal VATAmount { get; set; }
    public decimal VATRate { get; set; }
}