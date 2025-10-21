using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using fiskaltrust.Middleware.SCU.BE.ZwarteDoos.Models;
using Microsoft.Extensions.Logging;

namespace fiskaltrust.Middleware.SCU.BE.ZwarteDoos;

public class ZwarteDoosFactory
{
    private readonly ILogger<ZwarteDoosFactory> _logger;
    private readonly HttpClient _httpClient;
    private readonly ZwarteDoosScuConfiguration _configuration;

    public ZwarteDoosFactory(ILogger<ZwarteDoosFactory> logger, HttpClient httpClient, ZwarteDoosScuConfiguration configuration)
    {
        _logger = logger;
        _httpClient = httpClient;
        _configuration = configuration;
        
        ConfigureHttpClient();
    }

    private void ConfigureHttpClient()
    {
        var baseUrl = _configuration.SandboxMode 
            ? ZwarteDoosConstants.SandboxServiceUrl 
            : ZwarteDoosConstants.DefaultServiceUrl;
            
        _httpClient.BaseAddress = new Uri(baseUrl);
        _httpClient.DefaultRequestHeaders.Add(ZwarteDoosConstants.ApiKeyHeaderName, _configuration.ApiKey);
        _httpClient.Timeout = TimeSpan.FromSeconds(_configuration.TimeoutSeconds);
    }

    public async Task<ZwarteDoosInvoiceResponse> SubmitInvoiceAsync(ZwarteDoosInvoiceRequest request)
    {
        try
        {
            _logger.LogInformation("Submitting invoice {InvoiceNumber} to ZwarteDoos", request.InvoiceNumber);
            
            var json = JsonSerializer.Serialize(request, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
            
            var content = new StringContent(json, Encoding.UTF8, ZwarteDoosConstants.ContentTypeJson);
            
            var response = await _httpClient.PostAsync(ZwarteDoosConstants.InvoiceEndpoint, content);
            var responseContent = await response.Content.ReadAsStringAsync();
            
            if (response.IsSuccessStatusCode)
            {
                var result = JsonSerializer.Deserialize<ZwarteDoosInvoiceResponse>(responseContent, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });
                
                _logger.LogInformation("Successfully submitted invoice {InvoiceNumber}", request.InvoiceNumber);
                return result ?? new ZwarteDoosInvoiceResponse { Success = false, Errors = { "Failed to parse response" } };
            }
            else
            {
                _logger.LogError("Failed to submit invoice {InvoiceNumber}. Status: {StatusCode}, Response: {Response}", 
                    request.InvoiceNumber, response.StatusCode, responseContent);
                    
                return new ZwarteDoosInvoiceResponse
                {
                    Success = false,
                    Errors = { $"HTTP {response.StatusCode}: {responseContent}" }
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception occurred while submitting invoice {InvoiceNumber}", request.InvoiceNumber);
            return new ZwarteDoosInvoiceResponse
            {
                Success = false,
                Errors = { ex.Message }
            };
        }
    }

    public async Task<bool> CheckServiceStatusAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync(ZwarteDoosConstants.StatusEndpoint);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check ZwarteDoos service status");
            return false;
        }
    }
}