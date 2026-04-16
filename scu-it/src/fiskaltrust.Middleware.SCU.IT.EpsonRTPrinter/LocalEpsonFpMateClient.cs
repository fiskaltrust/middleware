using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace fiskaltrust.Middleware.SCU.IT.EpsonRTPrinter;

public class LocalEpsonFpMateClient : IEpsonFpMateClient
{
    private readonly HttpClient _httpClient;
    private readonly string _commandUrl;

    public LocalEpsonFpMateClient(HttpClient httpClient, EpsonRTPrinterSCUConfiguration configuration)
    {
        _httpClient = httpClient;

        if (string.IsNullOrEmpty(configuration.DeviceUrl))
        {
            throw new InvalidOperationException("EpsonScuConfiguration DeviceUrl not set.");
        }

        _commandUrl = $"cgi-bin/fpmate.cgi?timeout={configuration.ServerTimeoutMs}";
    }

    public async Task<HttpResponseMessage> SendCommandAsync(string content)
    {
        var response = await _httpClient.PostAsync(_commandUrl, new StringContent(content, Encoding.UTF8, "application/xml"));
        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException($"An error occured while sending a request to the Epson device (StatusCode: {response.StatusCode}, Content: {await response.Content.ReadAsStringAsync()})");
        }
        return response;
    }
}
