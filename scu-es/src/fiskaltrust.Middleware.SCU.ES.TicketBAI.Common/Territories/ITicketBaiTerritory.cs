using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using fiskaltrust.Middleware.SCU.ES.TicketBAI.Common.Models;

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

    public void AddHeaders(TicketBaiRequest request, HttpRequestHeaders headers);

    public ByteArrayContent GetContent(TicketBaiRequest request, string content);

    public Task<string> GetResponse(HttpResponseMessage response);
}
