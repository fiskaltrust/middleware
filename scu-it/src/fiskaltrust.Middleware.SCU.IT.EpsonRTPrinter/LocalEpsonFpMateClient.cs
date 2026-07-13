using System.Threading.Tasks;
using System.Net.Http;
using System;
using System.Text;

namespace fiskaltrust.Middleware.SCU.IT.EpsonRTPrinter;

public class LocalEpsonFpMateClient : IEpsonFpMateClient
{
    private readonly HttpClient _httpClient;
    private readonly string _commandUrl;

    public LocalEpsonFpMateClient(EpsonRTPrinterSCUConfiguration configuration) : this(configuration, new HttpClientHandler())
    {
    }

    internal LocalEpsonFpMateClient(EpsonRTPrinterSCUConfiguration configuration, HttpMessageHandler handler)
    {
        if (string.IsNullOrEmpty(configuration.DeviceUrl))
        {
            throw new NullReferenceException("EpsonScuConfiguration DeviceUrl not set.");
        }
        _httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri(configuration.DeviceUrl),
            Timeout = TimeSpan.FromMilliseconds(configuration.ClientTimeoutMs)
        };
        _commandUrl = $"cgi-bin/fpmate.cgi?timeout={configuration.ServerTimeoutMs}";
    }

    public async Task<HttpResponseMessage> SendCommandAsync(string content)
    {
        HttpResponseMessage response;
        try
        {
            response = await _httpClient.PostAsync(_commandUrl, new StringContent(content, Encoding.UTF8, "application/xml"));
        }
        catch (TaskCanceledException exception)
        {
            throw new EpsonNoResponseException("The command was dispatched to the Epson device, but no response was received.", exception);
        }
        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException($"An error occured while sending a request to the Epson device (StatusCode: {response.StatusCode}, Content: {await response.Content.ReadAsStringAsync()})");
        }
        return response;
    }
}
