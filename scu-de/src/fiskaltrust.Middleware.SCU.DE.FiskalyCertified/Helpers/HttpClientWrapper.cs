using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace fiskaltrust.Middleware.SCU.DE.FiskalyCertified.Helpers
{
    public sealed class HttpClientWrapper : IDisposable
    {
        public static string Timoutlog = "TimeoutException: The client did not response in the configured time!";
        private readonly HttpClient _httpClient;
        private readonly FiskalySCUConfiguration _configuration;
        private readonly ILogger _logger;

        public HttpClientWrapper (FiskalySCUConfiguration configuration, ILogger logger)
        {
            _configuration = configuration;
            _logger = logger;
            var url = configuration.ApiEndpoint.EndsWith("/") ? configuration.ApiEndpoint : $"{configuration.ApiEndpoint}/";
            _httpClient =  new HttpClient(new AuthenticatedHttpClientHandler(configuration, logger) { Proxy = ConfigurationHelper.CreateProxy(configuration) })
            {
                BaseAddress = new Uri(url),
                Timeout = TimeSpan.FromMilliseconds(configuration.FiskalyClientTimeout)
            };
        }

        public static async Task<T> WrapCall<T>(Task<T> task, int timeout, ILogger logger)       {
            if (RuntimeHelper.IsMono)
            {
                try
                {
                    var taskAsync = task;
                    var result = await Task.WhenAny(taskAsync, Task.Delay(TimeSpan.FromSeconds(timeout))).ConfigureAwait(false);
                    if (result == taskAsync)
                    {
                        return taskAsync.Result;
                    }
                    else
                    {
                        logger.LogError("Task finish: " + Timoutlog);
                        throw new TimeoutException();
                    }
                }
                catch (Exception e)
                {
                    logger?.LogError(e, e.Message);
                    if (e.InnerException?.Message != null && e.InnerException.Message.Equals("A task was canceled."))
                    {
                        logger?.LogError(Timoutlog);
                        throw new TimeoutException("The client did not response in the configured time!");
                    }
                   throw new Exception(e.Message);
                }
            } 
            else {
                return await task.ConfigureAwait(false);
            }
        }

        public async Task<HttpResponseMessage> PutAsync(string requestUri, HttpContent content) => await WrapCall(_httpClient.PutAsync(requestUri, content), _configuration.FiskalyClientTimeout, _logger).ConfigureAwait(false);

        public async Task<HttpResponseMessage> GetAsync(string requestUri) => await WrapCall(_httpClient.GetAsync(requestUri), _configuration.FiskalyClientTimeout, _logger).ConfigureAwait(false);

        public async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request) => await WrapCall(_httpClient.SendAsync(request), _configuration.FiskalyClientTimeout, _logger).ConfigureAwait(false);

        public async Task<HttpResponseMessage> PostAsync(string requestUri, HttpContent content) => await WrapCall(_httpClient.PostAsync(requestUri, content), _configuration.FiskalyClientTimeout, _logger).ConfigureAwait(false);

        public void Dispose() => _httpClient?.Dispose();
    }
}
