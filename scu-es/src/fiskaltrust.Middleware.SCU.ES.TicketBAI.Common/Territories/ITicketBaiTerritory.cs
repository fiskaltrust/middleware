using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using fiskaltrust.Middleware.SCU.ES.TicketBAI.Common.Models;
using Microsoft.Xades;

namespace fiskaltrust.Middleware.SCU.ES.TicketBAI.Common.Territories;

public interface ITicketBaiTerritory
{
    string PolicyIdentifier { get; }
    string PolicyDigest { get; }
    string Algorithm { get; }
    string ProdEndpoint { get; }
    string SandboxEndpoint { get; }
    string QrCodeValidationEndpoint { get; }
    string QrCodeSandboxValidationEndpoint { get; }
    string SubmitInvoices { get; }
    string CancelInvoices { get; }
    string SubmitZuzendu { get; }
    string CancelZuzendu { get; }

    public void AddHeaders(TicketBai request, HttpRequestHeaders headers);

    public string ProcessContent(TicketBai request, string content);

    public ByteArrayContent GetHttpContent(string content);

    public Task<(bool success, List<(string code, string message)> messages, string response)> GetSuccess(HttpResponseMessage response);
}
