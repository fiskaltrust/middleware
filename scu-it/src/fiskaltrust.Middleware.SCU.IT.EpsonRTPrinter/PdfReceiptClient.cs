using System;
using System.Net.Http;
using System.Threading.Tasks;
using fiskaltrust.Middleware.SCU.IT.EpsonRTPrinter.Models;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace fiskaltrust.Middleware.SCU.IT.EpsonRTPrinter;

public class PdfReceiptClient : IPdfReceiptClient
{
    private readonly HttpClient _httpClient;
    private readonly string? _baseUrl;
    private readonly ILogger<PdfReceiptClient> _logger;

    public PdfReceiptClient(HttpClient httpClient, EpsonRTPrinterSCUConfiguration configuration, ILogger<PdfReceiptClient> logger)
    {
        _httpClient = httpClient;
        _baseUrl = configuration.PdfServerUrl?.TrimEnd('/');
        _logger = logger;

        if (string.IsNullOrEmpty(_baseUrl))
        {
            _logger.LogDebug("PdfReceiptClient: PdfServerUrl not configured, PDF fetching disabled");
        }
    }

    public async Task<GetPdfResponse?> GetReceiptPdfAsync(string znum, string numdoc, string matricola, string date)
    {
        if (string.IsNullOrEmpty(_baseUrl))
        {
            return null;
        }

        var endpoint = $"{_baseUrl}/Fiskaltrust/getPDF.php?znum={Uri.EscapeDataString(znum)}&numdoc={Uri.EscapeDataString(numdoc)}&matricola={Uri.EscapeDataString(matricola)}&data={Uri.EscapeDataString(date)}";

        try
        {
            var result = await _httpClient.GetAsync(endpoint);
            var resultContent = await result.Content.ReadAsStringAsync();

            if (result.IsSuccessStatusCode)
            {
                return JsonConvert.DeserializeObject<GetPdfResponse>(resultContent);
            }

            _logger.LogWarning("PdfReceiptClient: request failed with status {StatusCode}: {Content}", result.StatusCode, resultContent);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "PdfReceiptClient: request failed for znum={ZNum} numdoc={NumDoc}", znum, numdoc);
            return null;
        }
    }
}
