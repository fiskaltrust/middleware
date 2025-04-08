using System.Threading.Tasks;
using System.Net.Http;
using System;
using System.Text;

namespace fiskaltrust.Middleware.SCU.IT.EpsonRTPrinter;

public class LocalEpsonFpMateClient : IEpsonFpMateClient
{
    private readonly HttpClient _httpClient;
    private readonly string _commandUrl;

    public LocalEpsonFpMateClient(EpsonRTPrinterSCUConfiguration configuration)
    {
        if (string.IsNullOrEmpty(configuration.DeviceUrl))
        {
            throw new NullReferenceException("EpsonScuConfiguration DeviceUrl not set.");
        }
        _httpClient = new HttpClient
        {
            BaseAddress = new Uri(configuration.DeviceUrl),

        };
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