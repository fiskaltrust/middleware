using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using fiskaltrust.Middleware.SCU.ES.TicketBAI.Models;
using fiskaltrust.Middleware.SCU.ES.TicketBAI.Territories;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

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
        var httpRequestHeaders = new HttpRequestMessage(HttpMethod.Post, new Uri(_ticketBaiTerritory.SandboxEndpoint + _ticketBaiTerritory.SubmitInvoices))
        {
            Content = new StringContent(content, Encoding.UTF8, "application/xml")
        };
        if(_configuration.TicketBaiTerritory == TicketBaiTerritory.Bizkaia)
        {
            httpRequestHeaders.Headers.Add("eus-bizkaia-n3-version", "1.0");
            httpRequestHeaders.Headers.Add("eus-bizkaia-n3-content-type", "application/xml");
            // TODO which year needs to be transmitted?
            httpRequestHeaders.Headers.Add("eus-bizkaia-n3-data", JsonConvert.SerializeObject(Bizkaia.GenerateHeader(ticketBaiRequest.Sujetos.Emisor.NIF, ticketBaiRequest.Sujetos.Emisor.ApellidosNombreRazonSocial, "240", DateTime.UtcNow.Year.ToString())));
        }
        
        var response = await _httpClient.SendAsync(httpRequestHeaders);
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

    public string GetRawXml(TicketBaiRequest ticketBaiRequest)
    {
        return _ticketBaiRequestFactory.CreateXadesSignedXmlContent(ticketBaiRequest); 
    }
}
