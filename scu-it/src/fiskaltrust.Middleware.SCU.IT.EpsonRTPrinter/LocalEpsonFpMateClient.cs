using System.Threading;
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
            Timeout = TimeSpan.FromMilliseconds(configuration.ClientTimeoutMs)
        };
        _commandUrl = $"cgi-bin/fpmate.cgi?timeout={configuration.ServerTimeoutMs}";
    }

    public async Task<HttpResponseMessage> SendCommandAsync(string content, TimeSpan? timeout = null)
    {
        // A shorter deadline for this one command; the client's own still applies as the upper bound. Cancelling
        // the token surfaces as TaskCanceledException, the same shape HttpClient raises when its timeout
        // elapses, so callers cannot tell the two apart and do not need to.
        using var cancellation = timeout.HasValue ? new CancellationTokenSource(timeout.Value) : null;
        var response = await _httpClient.PostAsync(_commandUrl, new StringContent(content, Encoding.UTF8, "application/xml"),
            cancellation?.Token ?? CancellationToken.None);
        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException($"An error occured while sending a request to the Epson device (StatusCode: {response.StatusCode}, Content: {await response.Content.ReadAsStringAsync()})");
        }
        return response;
    }
}