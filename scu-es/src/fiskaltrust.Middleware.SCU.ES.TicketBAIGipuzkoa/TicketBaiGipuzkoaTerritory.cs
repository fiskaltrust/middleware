using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using fiskaltrust.Middleware.SCU.ES.TicketBAI.Common;
using fiskaltrust.Middleware.SCU.ES.TicketBAI.Common.Helpers;
using fiskaltrust.Middleware.SCU.ES.TicketBAI.Common.Models;
using fiskaltrust.Middleware.SCU.ES.TicketBAI.Common.Territories;
using Microsoft.Xades;

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

    public string ProcessContent(TicketBaiRequest request, string content) => content;

    public ByteArrayContent GetHttpContent(string content) => new StringContent(content, Encoding.UTF8, "application/xml");

    public async Task<(bool success, List<(string code, string message)> messages, string response)> GetSuccess(HttpResponseMessage response)
    {
        var responseContent = await response.Content.ReadAsStringAsync();
        var ticketBaiResponse = XmlHelpers.ParseXML<TicketBaiResponse>(responseContent) ?? throw new Exception("Something horrible has happened");
        var isSuccess = ticketBaiResponse.Salida!.Estado == "00";
        return (isSuccess,
            isSuccess
                ? ticketBaiResponse.Salida?.ResultadosValidacion?.Select(x => (x.Codigo, x.Descripcion))?.ToList() ?? new()
                : ticketBaiResponse.Salida.ResultadosValidacion.Select(x => (x.Codigo, x.Descripcion)).ToList(),
            responseContent);
    }
}