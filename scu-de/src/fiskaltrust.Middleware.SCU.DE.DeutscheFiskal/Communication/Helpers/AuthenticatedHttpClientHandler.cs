using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using fiskaltrust.Middleware.SCU.DE.DeutscheFiskal.Exceptions;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace fiskaltrust.Middleware.SCU.DE.DeutscheFiskal.Communication
{
    public class AuthenticatedHttpClientHandler : HttpClientHandler
    {
        private const string ENDPOINT = "oauth/token";

        private readonly ClientConfiguration _config;
        private string _accessToken;
        private DateTime? _expiresOn;

        public AuthenticatedHttpClientHandler(ClientConfiguration config)
        {
            _config = config;
        }

        internal async Task<string> GetToken()
        {
            if (!IsTokenExpired())
            {
                return _accessToken;
            }

            using var client = new HttpClient() { BaseAddress = _config.BaseAddress };
            var credentials = Encoding.ASCII.GetBytes($"{_config.UserName}:{_config.Password}");
            var header = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(credentials));
            client.DefaultRequestHeaders.Authorization = header;

            var properties = _config.AdditionalProperties.Select(x => x).ToList();
            properties.Add(new KeyValuePair<string, string>("grant_type", _config.GrantType));
            
            var responseMessage = await client.PostAsync(ENDPOINT, new FormUrlEncodedContent(properties));

            if (!responseMessage.IsSuccessStatusCode)
            {
                var content = await responseMessage.Content.ReadAsStringAsync();
                throw new FiskalCloudException($"Could not get OAuth token from FCC (Status code: {responseMessage.StatusCode}, Response: {content})");
            }
            
            var responseContent = await responseMessage.Content.ReadAsStringAsync();
            var response = JsonConvert.DeserializeObject<OAuthTokenResponse>(responseContent);
            _accessToken = response.AccessToken;
            _expiresOn = DateTime.UtcNow.AddSeconds(response.ExpiresInSeconds * 0.9);

            return _accessToken;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", await GetToken().ConfigureAwait(false));
            return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
        }

        private bool IsTokenExpired() => _expiresOn == null || _expiresOn < DateTime.UtcNow;
    }
}
