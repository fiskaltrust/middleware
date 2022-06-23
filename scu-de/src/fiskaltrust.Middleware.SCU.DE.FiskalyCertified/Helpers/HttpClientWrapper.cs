using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace fiskaltrust.Middleware.SCU.DE.FiskalyCertified.Helpers
{
    public sealed class HttpClientWrapper : IDisposable
    {
        public static string TIMEOUT_LOG = "The client did not respond in the configured time!";
        private readonly HttpClient _httpClient;
        private readonly FiskalySCUConfiguration _configuration;

        public HttpClientWrapper(FiskalySCUConfiguration configuration)
        {
            _configuration = configuration;
            var url = configuration.ApiEndpoint.EndsWith("/") ? configuration.ApiEndpoint : $"{configuration.ApiEndpoint}/";
            _httpClient = new HttpClient(new AuthenticatedHttpClientHandler(configuration) { Proxy = ConfigurationHelper.CreateProxy(configuration) })
            {
                BaseAddress = new Uri(url),
                Timeout = TimeSpan.FromMilliseconds(configuration.FiskalyClientTimeout)
            };
        }

        public static async Task<T> WrapCall<T>(Task<T> task, int timeout)
        {
            if (RuntimeHelper.IsMono)
            {
                try
                {
                    var result = await Task.WhenAny(task, Task.Delay(TimeSpan.FromSeconds(timeout))).ConfigureAwait(false);
                    if (result == task)
                    {
                        return task.Result;
                    }
                    else
                    {
                        throw new TimeoutException(TIMEOUT_LOG + " (throwing away request task)");
                    }
                }
                catch (Exception e)
                {
                    if (e.InnerException?.Message != null && e.InnerException.Message.Equals("A task was canceled."))
                    {
                        throw new TimeoutException(TIMEOUT_LOG);
                    }
                    throw new Exception(e.Message);
                }
            }
            else
            {
                return await task.ConfigureAwait(false);
            }
        }

        public async Task<HttpResponseMessage> PutAsync(string requestUri, HttpContent content) => await WrapCall(_httpClient.PutAsync(requestUri, content), _configuration.FiskalyClientTimeout).ConfigureAwait(false);

        public async Task<HttpResponseMessage> GetAsync(string requestUri) => await WrapCall(_httpClient.GetAsync(requestUri), _configuration.FiskalyClientTimeout).ConfigureAwait(false);

        public async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request) => await WrapCall(_httpClient.SendAsync(request), _configuration.FiskalyClientTimeout).ConfigureAwait(false);

        public async Task<HttpResponseMessage> PostAsync(string requestUri, HttpContent content) => await WrapCall(_httpClient.PostAsync(requestUri, content), _configuration.FiskalyClientTimeout).ConfigureAwait(false);

        public void Dispose() => _httpClient?.Dispose();
    }
}
