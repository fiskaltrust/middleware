using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.Middleware.SCU.ES.TicketBAI.Models;

namespace fiskaltrust.Middleware.SCU.ES.TicketBAI;

public class SubmitResponse
{
    public string? RequestContent { get; set; }
    public string? ResponseContent { get; set; }
    public bool Succeeded { get; set; }
    public Uri? QrCode { get; set; }
    public string? ShortSignatureValue { get; set; }
    public SignatureType? SignatureValue { get; set; }
    public string? ExpeditionDate { get; set; }
    public string? IssuerVatId { get; set; }
    public string? Identifier { get; set; }
    public List<(string code, string message)> ResultMessages { get; set; } = new List<(string code, string message)>();
}