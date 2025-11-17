using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using fiskaltrust.Middleware.SCU.ES.TicketBAI.Common.Models;
using fiskaltrust.Middleware.SCU.ES.TicketBAI.Common.Territories;
using fiskaltrust.Middleware.SCU.ES.TicketBAI.Helpers;

namespace fiskaltrust.Middleware.SCU.ES.TicketBAIGipuzkoa;

public class TicketBaiGipuzkoaTerritory : ITicketBaiTerritory
{
    public string PolicyIdentifier => "https://www.gipuzkoa.eus/ticketbai/sinadura/";

    public string PolicyDigest => "vSe1CH7eAFVkGN0X2Y7Nl9XGUoBnziDA5BGUSsyt8mg=";

    public string Algorithm => "SHA256";

    public string ProdEndpoint => "https://tbai-z.egoitza.gipuzkoa.eus";

    public string SandboxEndpoint => "https://tbai-z.prep.gipuzkoa.eus";

    public string QrCodeValidationEndpoint => "https://tbai.prep.gipuzkoa.eus/qr/";

    public string QrCodeSandboxValidationEndpoint => "https://tbai.prep.gipuzkoa.eus/qr/";

    public string SubmitInvoices => "/sarrerak/alta";

    public string CancelInvoices => "/sarrerak/baja";

    public string SubmitZuzendu => "/sarrerak/zuzendu-alta";

    public string CancelZuzendu => "/sarrerak/zuzendu-baja";

    public void AddHeaders(TicketBaiRequest request, HttpRequestHeaders headers) { }
    public ByteArrayContent GetContent(TicketBaiRequest request, string content) => new StringContent(content, Encoding.UTF8, "application/xml");
    public async Task<Result<string, Result<string, Exception>>> GetResponse(HttpResponseMessage response) => await response.Content.ReadAsStringAsync();
}