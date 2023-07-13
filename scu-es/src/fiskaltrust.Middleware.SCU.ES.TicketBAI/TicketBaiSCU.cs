using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using fiskaltrust.Middleware.SCU.ES.TicketBAI.Models;
using Microsoft.Extensions.Logging;

namespace fiskaltrust.Middleware.SCU.ES.TicketBAI;

public sealed class TicketBaiSCU //: IESSSCD 
{
    private readonly TicketBaiSCUConfiguration _configuration;
    private readonly TicketBaiRequestFactory _ticketBaiRequestFactory;
    private readonly HttpClient _httpClient;
    private readonly ILogger<TicketBaiSCU> _logger;

    public TicketBaiSCU(ILogger<TicketBaiSCU> logger, TicketBaiSCUConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
        _ticketBaiRequestFactory = new TicketBaiRequestFactory(configuration);
        var handler = new HttpClientHandler();
        handler.ClientCertificates.Add(_configuration.Certificate);
        _httpClient = new HttpClient(handler)
        {
            BaseAddress = new System.Uri(Digests.Gipuzkoa.SANDBOX_ENDPOINT)
        };
    }

    public async Task<string> SubmitInvoiceAsync(TicketBaiRequest ticketBaiRequest)
    {
        var content = _ticketBaiRequestFactory.CreateXadesSignedXmlContent(ticketBaiRequest, Digests.Gipuzkoa.POLICY_IDENTIFIER, Digests.Gipuzkoa.POLICY_DIGEST, Digests.Gipuzkoa.POLICY_IDENTIFIER);
        var response = await _httpClient.PostAsync(Digests.Gipuzkoa.SUBMIT_INVOICES, new StringContent(content, Encoding.UTF8, "application/xml"));
        if (response.IsSuccessStatusCode)
        {
            _logger.LogInformation("Successful");
        }
        return await response.Content.ReadAsStringAsync();
    }

    public async Task<string> CancelInvoiceAsync(TicketBaiRequest ticketBaiRequest)
    {
        var content = _ticketBaiRequestFactory.CreateXadesSignedXmlContent(ticketBaiRequest, Digests.Gipuzkoa.POLICY_IDENTIFIER, Digests.Gipuzkoa.POLICY_DIGEST, Digests.Gipuzkoa.POLICY_IDENTIFIER);
        var response = await _httpClient.PostAsync(Digests.Gipuzkoa.CANCEL_INVOICES, new StringContent(content, Encoding.UTF8, "application/xml"));
        if (response.IsSuccessStatusCode)
        {
            _logger.LogInformation("Successful");
        }
        return await response.Content.ReadAsStringAsync();
    }
}
