using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using fiskaltrust.Middleware.SCU.ES.TicketBAI.Common;
using fiskaltrust.Middleware.SCU.ES.TicketBAI.Common.Helpers;
using fiskaltrust.Middleware.SCU.ES.TicketBAI.Common.Models;
using fiskaltrust.Middleware.SCU.ES.TicketBAI.Common.Territories;
using Microsoft.Xades;

namespace fiskaltrust.Middleware.SCU.ES.TicketBAIAraba;

public class TicketBaiArabaTerritory : ITicketBaiTerritory
{
    public string PolicyIdentifier => "https://ticketbai.araba.eus/tbai/sinadura/";

    public string PolicyDigest => "d69VEBc4ED4QbwnDtCA2JESgJiw+rwzfutcaSl5gYvM=";

    public string Algorithm => "SHA256";

    public string ProdEndpoint => "https://ticketbai.araba.eus/TicketBAI/v1";

    public string SandboxEndpoint => "https://pruebas-ticketbai.araba.eus/TicketBAI/v1";

    public string QrCodeValidationEndpoint => "https://ticketbai.araba.eus/tbai/qrtbai/";

    public string QrCodeSandboxValidationEndpoint => "https://pruebas-ticketbai.araba.eus/tbai/qrtbai/";

    public string SubmitInvoices => "/facturas";

    public string CancelInvoices => "/anulaciones";

    public string SubmitZuzendu => "/facturas/subsanarmodificar";

    public string CancelZuzendu => "/anulaciones/baja";

    public void AddHeaders(TicketBaiRequest request, HttpRequestHeaders headers) { }

    public string ProcessContent(TicketBaiRequest request, string content) => content;

    public ByteArrayContent GetHttpContent(string content) => new StringContent(content, Encoding.UTF8, "application/xml");

    public async Task<(bool success, List<(string code, string message)> messages, string response)> GetSuccess(HttpResponseMessage response)
    {
        var responseContent = await response.Content.ReadAsStringAsync();
        var ticketBaiResponse = XmlHelpers.ParseXML<TicketBaiResponse>(responseContent) ?? throw new Exception("Something horrible has happened");


        if (ticketBaiResponse.Salida!.Estado == "00")
        {
            return (true, ticketBaiResponse.Salida?.ResultadosValidacion?.Select(x => (x.Codigo, x.Descripcion))?.ToList() ?? new(), responseContent);
        }
        else
        {
            return (false, ticketBaiResponse.Salida.ResultadosValidacion.Select(x => (x.Codigo, x.Descripcion)).ToList(), responseContent);
        }
    }
}
