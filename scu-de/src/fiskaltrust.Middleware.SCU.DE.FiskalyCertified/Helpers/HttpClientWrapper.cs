using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using fiskaltrust.Middleware.SCU.DE.FiskalyCertified.Exceptions;
using Microsoft.Extensions.Logging;

namespace fiskaltrust.Middleware.SCU.DE.FiskalyCertified.Helpers
{
    public class HttpClientWrapper : IDisposable
    {
        public static string TIMEOUT_LOG = "The client did not respond in the configured time!";
        private readonly HttpClient _httpClient;
        private readonly FiskalySCUConfiguration _configuration;
        private readonly ConcurrentDictionary<Guid, int> _5xxRetries = new ConcurrentDictionary<Guid, int>();
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

        public async Task<HttpResponseMessage> PutAsync(string requestUri, HttpContent content, Guid requestId)
        {
            var response = await WrapCall(_httpClient.PutAsync(requestUri, content), _configuration.FiskalyClientTimeout).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                if(RetryNeeded((int) response.StatusCode, _5xxRetries, _configuration.RetriesOn5xxError, _logger, requestId))
                {
                    await PutAsync(requestUri, content, requestId).ConfigureAwait(false);
                }
                var responseContent = await response.Content.ReadAsStringAsync();
                throw new FiskalyException($"Communication error ({response.StatusCode}) while setting TSS metadata ({requestUri}). Response: {responseContent}",
                    (int) response.StatusCode, content.ToString());
            }
            else
            {
                _5xxRetries.TryRemove(requestId, out _);
            }
            return response;
        }

        public async Task<HttpResponseMessage> GetAsync(string requestUri, Guid requestId)
        {
            var response = await WrapCall(_httpClient.GetAsync(requestUri), _configuration.FiskalyClientTimeout).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                if (RetryNeeded((int) response.StatusCode, _5xxRetries, _configuration.RetriesOn5xxError, _logger, requestId))
                {
                    await GetAsync(requestUri, requestId).ConfigureAwait(false);
                }
                var responseContent = await response.Content.ReadAsStringAsync();
                throw new FiskalyException($"Communication error ({response.StatusCode}) while getting TSS metadata ({requestUri}). Response: {responseContent}",
                    (int) response.StatusCode, requestUri);
            }
            else
            {
                _5xxRetries.TryRemove(requestId, out _);
            }
            return response;
        }

        public async Task<HttpResponseMessage> SendAsync(HttpMethod httpMethod, string requestUri, string jsonPayload, Guid requestId)
        {
            var request = new HttpRequestMessage(httpMethod, requestUri)
            {
                Content = new StringContent(jsonPayload, Encoding.UTF8, "application/json")
            };
            var response = await WrapCall(_httpClient.SendAsync(request), _configuration.FiskalyClientTimeout).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                if (RetryNeeded((int) response.StatusCode, _5xxRetries, _configuration.RetriesOn5xxError, _logger, requestId))
                {
                    await SendAsync(httpMethod, requestUri, jsonPayload, requestId).ConfigureAwait(false);
                }
                var responseContent = await response.Content.ReadAsStringAsync();
                throw new FiskalyException($"Communication error ({response.StatusCode}) while setting TSS metadata ({request}). Response: {responseContent}",
                    (int) response.StatusCode, request.Content.ToString());
            }
            else
            {
                _5xxRetries.TryRemove(requestId, out _);
            }
            return response;
        }
            
        public async Task<HttpResponseMessage> PostAsync(string requestUri, HttpContent content, Guid requestId)
        {
            var response = await WrapCall(_httpClient.PostAsync(requestUri, content), _configuration.FiskalyClientTimeout).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                if (RetryNeeded((int) response.StatusCode, _5xxRetries, _configuration.RetriesOn5xxError, _logger, requestId))
                {
                    await PostAsync(requestUri, content, requestId).ConfigureAwait(false);
                }
                var responseContent = await response.Content.ReadAsStringAsync();
                throw new FiskalyException($"Communication error ({response.StatusCode}) while setting TSS metadata ({requestUri}). Response: {responseContent}",
                    (int) response.StatusCode, requestUri);
            }
            else
            {
                _5xxRetries.TryRemove(requestId, out _);
            }
            return response;
        }

        public static bool RetryNeeded(int statusCode, ConcurrentDictionary<Guid, int> retries,int configRetries, ILogger logger, Guid requestId)
        {
            if (statusCode >= 500 && statusCode <= 599)
            {
                if (retries.TryGetValue(requestId, out var doneRetries))
                {
                    if (configRetries > doneRetries)
                    {
                        doneRetries++;
                    }
                    else
                    {
                        retries.TryRemove(requestId, out _);
                        return false;
                    }
                }
                else
                {
                    doneRetries = 0;
                }
                retries.AddOrUpdate(requestId, doneRetries, (key, oldValue) => doneRetries);
                logger.LogInformation($"HttpStatusCode {statusCode} from Fiskaly retry {doneRetries} from {configRetries}");
                Thread.Sleep(1000 * (doneRetries + 1));
                return true;
            }
            return false;
        }

        public void Dispose() => _httpClient?.Dispose();

    }
}
