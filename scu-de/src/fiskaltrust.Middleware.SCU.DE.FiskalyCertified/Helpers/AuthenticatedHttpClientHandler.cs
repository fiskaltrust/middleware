using System;
using System.Collections.Concurrent;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using fiskaltrust.Middleware.SCU.DE.FiskalyCertified.Exceptions;
using fiskaltrust.Middleware.SCU.DE.FiskalyCertified.Models;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Org.BouncyCastle.Asn1.Ocsp;

namespace fiskaltrust.Middleware.SCU.DE.FiskalyCertified.Helpers
{
    public class AuthenticatedHttpClientHandler : HttpClientHandler
    {
        private const string ENDPOINT = "auth";

        private readonly ConcurrentDictionary<Guid, int> _5xxRetries = new ConcurrentDictionary<Guid, int>();
        private readonly FiskalySCUConfiguration _config;
        private readonly ILogger _logger;
        private string _accessToken;
        private DateTime? _expiresOn;

        public AuthenticatedHttpClientHandler(FiskalySCUConfiguration config, ILogger logger)
        {
            _logger = logger;
            _config = config;
        }

        internal async Task<string> GetToken()
        {
            if (!IsTokenExpired())
            {
                return _accessToken;
            }

            var url = _config.ApiEndpoint.EndsWith("/") ? _config.ApiEndpoint : $"{_config.ApiEndpoint}/";
            using var client = new HttpClient(new HttpClientHandler { Proxy = ConfigurationHelper.CreateProxy(_config) }, disposeHandler: true)
            {
                BaseAddress = new Uri(url),
                Timeout = TimeSpan.FromMilliseconds(_config.FiskalyClientTimeout)
            };

            var requestObject = new TokenRequestDto
            {
                ApiKey = _config.ApiKey,
                ApiSecret = _config.ApiSecret
            };

            var requestContent = JsonConvert.SerializeObject(requestObject);
            var responseMessage = await PostAsync(client, requestContent, Guid.NewGuid()).ConfigureAwait(false);

            var responseContent = await responseMessage.Content.ReadAsStringAsync();
            var response = JsonConvert.DeserializeObject<TokenResponseDto>(responseContent);
            _accessToken = response.AccessToken;
            _expiresOn = DateTime.UtcNow.AddSeconds(response.ExpiresInSeconds * 0.9);

            return _accessToken;
        }

        private async Task<HttpResponseMessage> PostAsync(HttpClient client, string requestContent, Guid requestId)
        {
            var responseMessage = await HttpClientWrapper.WrapCall(client.PostAsync(ENDPOINT, new StringContent(requestContent, Encoding.UTF8, "application/json")), _config.FiskalyClientTimeout).ConfigureAwait(false);

            if (!responseMessage.IsSuccessStatusCode)
            {
                if (HttpClientWrapper.RetryNeeded((int) responseMessage.StatusCode, _5xxRetries, _config.RetriesOn5xxError, _logger, requestId))
                {
                    await PostAsync(client, requestContent, requestId).ConfigureAwait(false);
                }
                var content = await responseMessage.Content.ReadAsStringAsync();
                throw new FiskalyException($"Could not get OAuth token from Fiskaly API (Status code: {responseMessage.StatusCode}, Response: {content})");
            }

            return responseMessage;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", await GetToken().ConfigureAwait(false));
            return await HttpClientWrapper.WrapCall(base.SendAsync(request, cancellationToken), _config.FiskalyClientTimeout).ConfigureAwait(false);
        }

        private bool IsTokenExpired() => _expiresOn == null || _expiresOn < DateTime.UtcNow;
    }
}
