using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
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

        public async Task<HttpResponseMessage> PutAsync(string requestUri, HttpContent content, int i = 0)
        {
            var response = await WrapCall(_httpClient.PutAsync(requestUri, content), _configuration.FiskalyClientTimeout).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                if ((int)response.StatusCode >= 500 && (int) response.StatusCode <= 599 && _configuration.RetriesOn5xxError > i)
                {
                    i++;
                    Thread.Sleep(1000 * (i + 1));
                    _logger.LogInformation($"HttpStatusCode {response.StatusCode} from Fiskaly retry {i} from {_configuration.RetriesOn5xxError}");
                    await PutAsync(requestUri, content, i).ConfigureAwait(false);
                }
                var responseContent = await response.Content.ReadAsStringAsync();
                throw new FiskalyException($"Communication error ({response.StatusCode}) while setting TSS metadata ({requestUri}). Response: {responseContent}",
                    (int) response.StatusCode, content.ToString());
            }
            return response;
        }

        public async Task<HttpResponseMessage> GetAsync(string requestUri, int i = 0)
        {
            var response = await WrapCall(_httpClient.GetAsync(requestUri), _configuration.FiskalyClientTimeout).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                if ((int) response.StatusCode >= 500 && (int) response.StatusCode <= 599 && _configuration.RetriesOn5xxError > i)
                {
                    i++;
                    Thread.Sleep(1000 * (i + 1));
                    _logger.LogInformation($"HttpStatusCode {response.StatusCode} from Fiskaly retry {i} from {_configuration.RetriesOn5xxError}");
                    await GetAsync(requestUri, i).ConfigureAwait(false);
                }
                var responseContent = await response.Content.ReadAsStringAsync();
                throw new FiskalyException($"Communication error ({response.StatusCode}) while getting TSS metadata ({requestUri}). Response: {responseContent}",
                    (int) response.StatusCode, requestUri);
            }
            return response;
        }

        public async Task<HttpResponseMessage> SendAsync(HttpMethod httpMethod, string requestUri, string jsonPayload, int i = 0)
        {
            var request = new HttpRequestMessage(httpMethod, requestUri)
            {
                Content = new StringContent(jsonPayload, Encoding.UTF8, "application/json")
            };
            var response = await WrapCall(_httpClient.SendAsync(request), _configuration.FiskalyClientTimeout).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                if ((int) response.StatusCode >= 500 && (int) response.StatusCode <= 599 && _configuration.RetriesOn5xxError > i)
                {
                    i++;
                    Thread.Sleep(1000 * (i + 1));
                    _logger.LogInformation($"HttpStatusCode {response.StatusCode} from Fiskaly retry {i} from {_configuration.RetriesOn5xxError}");
                    await SendAsync(httpMethod, requestUri, jsonPayload, i).ConfigureAwait(false);
                }
                var responseContent = await response.Content.ReadAsStringAsync();
                throw new FiskalyException($"Communication error ({response.StatusCode}) while setting TSS metadata ({request}). Response: {responseContent}",
                    (int) response.StatusCode, request.Content.ToString());
            }
            return response;
        }
            
        public async Task<HttpResponseMessage> PostAsync(string requestUri, HttpContent content, int i = 0)
        {
            var response = await WrapCall(_httpClient.PostAsync(requestUri, content), _configuration.FiskalyClientTimeout).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                if ((int) response.StatusCode >= 500 && (int) response.StatusCode <= 599 && _configuration.RetriesOn5xxError > i)
                {
                    i++;
                    Thread.Sleep(1000 * (i + 1));
                    _logger.LogInformation($"HttpStatusCode {response.StatusCode} from Fiskaly retry {i} from {_configuration.RetriesOn5xxError}");
                    await PostAsync(requestUri, content, i).ConfigureAwait(false);
                }
                var responseContent = await response.Content.ReadAsStringAsync();
                throw new FiskalyException($"Communication error ({response.StatusCode}) while setting TSS metadata ({requestUri}). Response: {responseContent}",
                    (int) response.StatusCode, requestUri);
            }
            return response;
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
