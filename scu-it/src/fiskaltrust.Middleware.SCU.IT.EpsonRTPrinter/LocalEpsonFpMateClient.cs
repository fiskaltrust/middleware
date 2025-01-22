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

    public async Task<HttpResponseMessage> SendCommandAsync(string payload)
    {
        var response = await _httpClient.PostAsync(_commandUrl, new StringContent(payload, Encoding.UTF8, "application/xml"));
        return response;
    }
}