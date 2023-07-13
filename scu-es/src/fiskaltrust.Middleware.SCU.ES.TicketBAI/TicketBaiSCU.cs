using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using fiskaltrust.Middleware.SCU.ES.TicketBAI.Helpers;
using fiskaltrust.Middleware.SCU.ES.TicketBAI.Models;
using fiskaltrust.Middleware.SCU.ES.TicketBAI.Territories;
using Microsoft.Extensions.Logging;

#pragma warning disable IDE0052

namespace fiskaltrust.Middleware.SCU.ES.TicketBAI;

public class TicketBaiSCU : IESSSCD 
{
    private readonly TicketBaiSCUConfiguration _configuration;
    private readonly TicketBaiRequestFactory _ticketBaiRequestFactory;
    private readonly HttpClient _httpClient;
    private readonly ILogger<TicketBaiSCU> _logger;
    private readonly ITicketBaiTerritory _ticketBaiTerritory;

    public TicketBaiSCU(ILogger<TicketBaiSCU> logger, TicketBaiSCUConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
        _ticketBaiRequestFactory = new TicketBaiRequestFactory(configuration);
        _ticketBaiTerritory = configuration.TicketBaiTerritory switch
        {
            TicketBaiTerritory.Araba => new Araba(),
            TicketBaiTerritory.Bizkaia => new Bizkaia(),
            TicketBaiTerritory.Gipuzkoa => new Gipuzkoa(),
            _ => throw new Exception("Not supported"),
        };

        var handler = new HttpClientHandler();
        handler.ClientCertificates.Add(_configuration.Certificate);

        _httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri(_ticketBaiTerritory.SandboxEndpoint)
        };
    }

    public async Task<SubmitResponse> SubmitInvoiceAsync(TicketBaiRequest ticketBaiRequest)
    {
        var content = _ticketBaiRequestFactory.CreateXadesSignedXmlContent(ticketBaiRequest);
        var response = await _httpClient.PostAsync(_ticketBaiTerritory.SubmitInvoices, new StringContent(content, Encoding.UTF8, "application/xml"));
        var responseContent = await response.Content.ReadAsStringAsync();
        return _ticketBaiRequestFactory.GetResponseFromContent(responseContent);
    }

    public async Task<SubmitResponse> CancelInvoiceAsync(TicketBaiRequest ticketBaiRequest)
    {
        var content = _ticketBaiRequestFactory.CreateXadesSignedXmlContent(ticketBaiRequest);
        var response = await _httpClient.PostAsync(_ticketBaiTerritory.CancelInvoices, new StringContent(content, Encoding.UTF8, "application/xml"));
        var responseContent = await response.Content.ReadAsStringAsync();
        return _ticketBaiRequestFactory.GetResponseFromContent(responseContent);
    }
}
