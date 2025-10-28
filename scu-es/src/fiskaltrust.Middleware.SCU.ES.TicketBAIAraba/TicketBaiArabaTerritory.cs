using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using fiskaltrust.Middleware.SCU.ES.TicketBAI.Common.Models;
using fiskaltrust.Middleware.SCU.ES.TicketBAI.Common.Territories;

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
    public ByteArrayContent GetContent(TicketBaiRequest request, string content) => new StringContent(content, Encoding.UTF8, "application/xml");
}
