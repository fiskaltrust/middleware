using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace fiskaltrust.Middleware.SCU.DE.FiskalyCertified.Helpers
{
    public sealed class HttpClientWrapper : IDisposable
    {
        private readonly HttpClient _httpClient;
        private readonly FiskalySCUConfiguration _configuration;

        public HttpClientWrapper (FiskalySCUConfiguration configuration)
        {
            _configuration = configuration;
            var url = configuration.ApiEndpoint.EndsWith("/") ? configuration.ApiEndpoint : $"{configuration.ApiEndpoint}/";
            _httpClient =  new HttpClient(new AuthenticatedHttpClientHandler(configuration) { Proxy = ConfigurationHelper.CreateProxy(configuration) })
            {
                BaseAddress = new Uri(url),
                Timeout = TimeSpan.FromMilliseconds(configuration.FiskalyClientTimeout)
            };
        }
        public async Task<HttpResponseMessage> PutAsync(string requestUri, HttpContent content)
        {
            if (RuntimeHelper.IsMono)
            {
                var putAsync = _httpClient.PutAsync(requestUri, content);
                var result = await Task.WhenAny(putAsync, Task.Delay(TimeSpan.FromSeconds(_configuration.FiskalyClientTimeout))).ConfigureAwait(false);
                if (result == putAsync)
                {
                    return putAsync.Result;
                }
                else
                {
                    throw new TimeoutException("The client did not response in the configured timeout!");
                }
            }
            return await _httpClient.PutAsync(requestUri, content).ConfigureAwait(false);
        }
        public async Task<HttpResponseMessage> GetAsync(string requestUri)
        {
            if (RuntimeHelper.IsMono)
            {
                var getAsync = _httpClient.GetAsync(requestUri);
                var result = await Task.WhenAny(getAsync, Task.Delay(TimeSpan.FromSeconds(_configuration.FiskalyClientTimeout))).ConfigureAwait(false);
                if (result == getAsync)
                {
                    return getAsync.Result;
                }
                else
                {
                    throw new TimeoutException("The client did not response in the configured timeout!");
                }
            }
            return await _httpClient.GetAsync(requestUri).ConfigureAwait(false);
        }
        public async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request)
        {
            if (RuntimeHelper.IsMono)
            {
                var sendAsync = _httpClient.SendAsync(request);
                var result = await Task.WhenAny(sendAsync, Task.Delay(TimeSpan.FromSeconds(_configuration.FiskalyClientTimeout))).ConfigureAwait(false);
                if (result == sendAsync)
                {
                    return sendAsync.Result;
                }
                else
                {
                    throw new TimeoutException("The client did not response in the configured timeout!");
                }
            }
            return await _httpClient.SendAsync(request).ConfigureAwait(false);
        }

        public async Task<HttpResponseMessage> PostAsync(string requestUri, HttpContent content)
        {
            if (RuntimeHelper.IsMono)
            {
                var postAsync = _httpClient.PostAsync(requestUri, content);
                var result = await Task.WhenAny(postAsync, Task.Delay(TimeSpan.FromSeconds(_configuration.FiskalyClientTimeout))).ConfigureAwait(false);
                if (result == postAsync)
                {
                    return postAsync.Result;
                }
                else
                {
                    throw new TimeoutException("The client did not response in the configured timeout!");
                }
            }
            return await _httpClient.PostAsync(requestUri, content).ConfigureAwait(false);
        }

        public void Dispose() => _httpClient?.Dispose();
    }
}
