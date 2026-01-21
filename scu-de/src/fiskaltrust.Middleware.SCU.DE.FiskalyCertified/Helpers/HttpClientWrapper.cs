using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using fiskaltrust.Middleware.SCU.DE.FiskalyCertified.Exceptions;
using Microsoft.Extensions.Logging;

namespace fiskaltrust.Middleware.SCU.DE.FiskalyCertified.Helpers
{
    public class HttpClientWrapper : IDisposable
    {
        private bool isDisposed;
        public static string TIMEOUT_LOG = "The client did not respond in the configured time!";
        private readonly HttpClient _httpClient;
        private readonly FiskalySCUConfiguration _configuration;
        private readonly ILogger<HttpClientWrapper> _logger;

        public HttpClientWrapper(FiskalySCUConfiguration configuration, ILogger<HttpClientWrapper> logger, HttpClient httpClient)
        {
            _logger = logger;
            _configuration = configuration;
            _httpClient = httpClient;
        }

        public HttpClientWrapper(FiskalySCUConfiguration configuration, ILogger<HttpClientWrapper> logger)
        {
            _logger = logger;
            _configuration = configuration;
            var url = configuration.ApiEndpoint.EndsWith("/") ? configuration.ApiEndpoint : $"{configuration.ApiEndpoint}/";
            _httpClient = new HttpClient(new AuthenticatedHttpClientHandler(configuration, _logger) { Proxy = ConfigurationHelper.CreateProxy(configuration) })
            {
                BaseAddress = new Uri(url),
                Timeout = TimeSpan.FromMilliseconds(configuration.FiskalyClientTimeout)
            };
        }
        /// <summary>
        /// This is needed for Mono to react on the client timeout. 
        /// </summary>
        public static async Task<T> WrapCall<T>(Task<T> task, int timespan)
        {
            if (RuntimeHelper.IsMono)
            {
                try
                {
                    var result = await Task.WhenAny(task, Task.Delay(TimeSpan.FromSeconds(timespan))).ConfigureAwait(false);
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
                    if (e.InnerException != null && e.InnerException is TaskCanceledException)
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

        public async Task<HttpResponseMessage> PutAsync(string requestUri, HttpContent content, int currentTry = 0)
        {
            var response = await WrapCall(_httpClient.PutAsync(requestUri, content), _configuration.FiskalyClientTimeout).ConfigureAwait(false);
            if (DoRetry(response, currentTry))
            {
                currentTry++;
                var retryAfterMs = RetryAfterMs(response);
                LogRetry(response.StatusCode, currentTry, retryAfterMs);
                await Task.Delay(retryAfterMs).ConfigureAwait(false);
                return await PutAsync(requestUri, content, currentTry).ConfigureAwait(false);
            }
            return response;
        }

        public async Task<HttpResponseMessage> GetAsync(string requestUri, int currentTry = 0)
        {
            var response = await WrapCall(_httpClient.GetAsync(requestUri), _configuration.FiskalyClientTimeout).ConfigureAwait(false);
            if (DoRetry(response, currentTry))
            {
                currentTry++;
                var retryAfterMs = RetryAfterMs(response);
                LogRetry(response.StatusCode, currentTry, retryAfterMs);
                await Task.Delay(retryAfterMs).ConfigureAwait(false);
                return await GetAsync(requestUri, currentTry).ConfigureAwait(false);
            }
            return response;
        }

        public async Task<HttpResponseMessage> SendAsync(HttpMethod httpMethod, string requestUri, string jsonPayload, int currentTry = 0)
        {
            var request = new HttpRequestMessage(httpMethod, requestUri)
            {
                Content = new StringContent(jsonPayload, Encoding.UTF8, "application/json")
            };
            var response = await WrapCall(_httpClient.SendAsync(request), _configuration.FiskalyClientTimeout).ConfigureAwait(false);
            if (DoRetry(response, currentTry))
            {
                currentTry++;
                var retryAfterMs = RetryAfterMs(response);
                LogRetry(response.StatusCode, currentTry, retryAfterMs);
                await Task.Delay(retryAfterMs).ConfigureAwait(false);
                return await SendAsync(httpMethod, requestUri, jsonPayload, currentTry).ConfigureAwait(false);
            }
            return response;
        }

        public async Task<HttpResponseMessage> PostAsync(string requestUri, HttpContent content, int currentTry = 0)
        {
            var response = await WrapCall(_httpClient.PostAsync(requestUri, content), _configuration.FiskalyClientTimeout).ConfigureAwait(false);
            if (DoRetry(response, currentTry))
            {
                currentTry++;
                var retryAfterMs = RetryAfterMs(response);
                LogRetry(response.StatusCode, currentTry, retryAfterMs);
                await Task.Delay(retryAfterMs).ConfigureAwait(false);
                return await PostAsync(requestUri, content, currentTry).ConfigureAwait(false);
            }
            return response;
        }

        private bool DoRetry(HttpResponseMessage response, int currentTry)
        {
            if (!((int) response.StatusCode >= 200 && (int) response.StatusCode <= 299)  && _configuration.RetriesOn5xxError > currentTry)
            {
                return true;
            }
            return false;
        }

        private void LogRetry(HttpStatusCode statusCode, int currentTry, int retryAfterMs)
        {
            _logger.LogWarning($"HttpStatusCode {statusCode} from Fiskaly retry {currentTry} from {_configuration.RetriesOn5xxError}, DelayOnRetriesInMs: {retryAfterMs}.");

        }

        private int RetryAfterMs(HttpResponseMessage response)
        {
            var retryAfterMs = _configuration.DelayOnRetriesInMs;
            if (response.Headers.TryGetValues("Retry-After", out var values))
            {
                var value = values.FirstOrDefault();
                if (int.TryParse(value, out var seconds))
                {
                    retryAfterMs = seconds*1000;
                }
            }
            return retryAfterMs;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        // The bulk of the clean-up code is implemented in Dispose(bool)
        protected virtual void Dispose(bool disposing)
        {
            if (isDisposed)
            {
                return;
            }

            if (disposing)
            {
                // free managed resources
                _httpClient?.Dispose();
            }
            isDisposed = true;
        }
    }
}
