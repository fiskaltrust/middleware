﻿using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using fiskaltrust.Middleware.SCU.DE.SwissbitCloudV2.Exceptions;
using Microsoft.Extensions.Logging;

namespace fiskaltrust.Middleware.SCU.DE.SwissbitCloudV2.Helpers
{
    public class HttpClientWrapper : IDisposable
    {
        private bool isDisposed;
        public static string TIMEOUT_LOG = "The client did not respond in the configured time!";
        private readonly HttpClient _httpClient;
        private readonly SwissbitCloudV2SCUConfiguration _configuration;
        private readonly ILogger<HttpClientWrapper> _logger;

        public HttpClientWrapper(SwissbitCloudV2SCUConfiguration configuration, ILogger<HttpClientWrapper> logger)
        {
            _logger = logger;
            _configuration = configuration;
            _httpClient = GetClient();
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
            var response = await WrapCall(_httpClient.PutAsync(requestUri, content), _configuration.SwissbitCloudV2Timeout).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                if ((int)response.StatusCode >= 500 && (int) response.StatusCode <= 599 && _configuration.RetriesOn5xxError > currentTry)
                {
                    currentTry++;
                    await Task.Delay(_configuration.DelayOnRetriesInMs * (currentTry + 1)).ConfigureAwait(false);
                    _logger.LogWarning($"HttpStatusCode {response.StatusCode} from SwissbitCloudV2 retry {currentTry} from {_configuration.RetriesOn5xxError}, DelayOnRetriesInMs: { _configuration.DelayOnRetriesInMs}.");
                    await PutAsync(requestUri, content, currentTry).ConfigureAwait(false);
                }
                var responseContent = await response.Content.ReadAsStringAsync();
                throw new SwissbitCloudV2Exception($"Communication error ({response.StatusCode}) while executing PUT {requestUri}. Response: {responseContent}",
                    (int) response.StatusCode, content.ToString());
            }
            return response;
        }

        public async Task<HttpResponseMessage> GetAsync(string requestUri, int currentTry = 0)
        {
            var response = await WrapCall(_httpClient.GetAsync(requestUri), _configuration.SwissbitCloudV2Timeout).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                if ((int) response.StatusCode >= 500 && (int) response.StatusCode <= 599 && _configuration.RetriesOn5xxError > currentTry)
                {
                    currentTry++;
                    await Task.Delay(_configuration.DelayOnRetriesInMs * (currentTry + 1)).ConfigureAwait(false);
                    _logger.LogWarning($"HttpStatusCode {response.StatusCode} from SwissbitCloudV2 retry {currentTry} from {_configuration.RetriesOn5xxError}, DelayOnRetriesInMs: { _configuration.DelayOnRetriesInMs}.");
                    await GetAsync(requestUri, currentTry).ConfigureAwait(false);
                }
                var responseContent = await response.Content.ReadAsStringAsync();
                throw new SwissbitCloudV2Exception($"Communication error ({response.StatusCode}) while executing GET {requestUri}. Response: {responseContent}",
                    (int) response.StatusCode, requestUri);
            }
            return response;
        }

        public async Task<HttpResponseMessage> SendAsync(HttpMethod httpMethod, string requestUri, string jsonPayload, int currentTry = 0)
        {
            var request = new HttpRequestMessage(httpMethod, requestUri)
            {
                Content = new StringContent(jsonPayload, Encoding.UTF8, "application/json")
            };
            var response = await WrapCall(_httpClient.SendAsync(request), _configuration.SwissbitCloudV2Timeout).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                if ((int) response.StatusCode >= 500 && (int) response.StatusCode <= 599 && _configuration.RetriesOn5xxError > currentTry)
                {
                    currentTry++;
                    await Task.Delay(_configuration.DelayOnRetriesInMs * (currentTry + 1)).ConfigureAwait(false);
                    _logger.LogWarning($"HttpStatusCode {response.StatusCode} from SwissbitCloudV2 retry {currentTry} from {_configuration.RetriesOn5xxError}, DelayOnRetriesInMs: { _configuration.DelayOnRetriesInMs}.");
                    await SendAsync(httpMethod, requestUri, jsonPayload, currentTry).ConfigureAwait(false);
                }
                var responseContent = await response.Content.ReadAsStringAsync();
                throw new SwissbitCloudV2Exception($"Communication error ({response.StatusCode}) while executing {httpMethod.Method} {request}. Response: {responseContent}",
                    (int) response.StatusCode, request.Content.ToString());
            }
            return response;
        }
            
        public async Task<HttpResponseMessage> PostAsync(string requestUri, HttpContent content, int currentTry = 0)
        {
            var response = await WrapCall(_httpClient.PostAsync(requestUri, content), _configuration.SwissbitCloudV2Timeout).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                if ((int) response.StatusCode >= 500 && (int) response.StatusCode <= 599 && _configuration.RetriesOn5xxError > currentTry)
                {
                    currentTry++;
                    await Task.Delay(_configuration.DelayOnRetriesInMs * (currentTry + 1)).ConfigureAwait(false);
                    _logger.LogWarning($"HttpStatusCode {response.StatusCode} from SwissbitCloudV2 retry {currentTry} from {_configuration.RetriesOn5xxError}, DelayOnRetriesInMs: { _configuration.DelayOnRetriesInMs}.");
                    await PostAsync(requestUri, content, currentTry).ConfigureAwait(false);
                }
                var responseContent = await response.Content.ReadAsStringAsync();
                throw new SwissbitCloudV2Exception($"Communication error ({response.StatusCode}) while executing POST {requestUri}. Response: {responseContent}",
                    (int) response.StatusCode, requestUri);
            }
            return response;
        }

        public async Task<HttpResponseMessage> DeleteAsync(string requestUri, int currentTry = 0)
        {
            var response = await WrapCall(_httpClient.DeleteAsync(requestUri), _configuration.SwissbitCloudV2Timeout).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                if ((int) response.StatusCode >= 500 && (int) response.StatusCode <= 599 && _configuration.RetriesOn5xxError > currentTry)
                {
                    currentTry++;
                    await Task.Delay(_configuration.DelayOnRetriesInMs * (currentTry + 1)).ConfigureAwait(false);
                    _logger.LogWarning($"HttpStatusCode {response.StatusCode} from SwissbitCloudV2 retry {currentTry} from {_configuration.RetriesOn5xxError}, DelayOnRetriesInMs: {_configuration.DelayOnRetriesInMs}.");
                    await DeleteAsync(requestUri, currentTry).ConfigureAwait(false);
                }
                var responseContent = await response.Content.ReadAsStringAsync();
                throw new SwissbitCloudV2Exception($"Communication error ({response.StatusCode}) while executing DELETE {requestUri}. Response: {responseContent}");
            }
            return response;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private HttpClient GetClient()
        {
            var client = new HttpClient(new HttpClientHandler { Proxy = ConfigurationHelper.CreateProxy(_configuration) })
            {
                BaseAddress = new Uri(_configuration.ApiEndpoint),
                Timeout = TimeSpan.FromMilliseconds(_configuration.SwissbitCloudV2Timeout)
            };
            var credentials = Encoding.ASCII.GetBytes($"{_configuration.TseSerialNumber}:{_configuration.TseAccessToken}");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(credentials));

            return client;
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
