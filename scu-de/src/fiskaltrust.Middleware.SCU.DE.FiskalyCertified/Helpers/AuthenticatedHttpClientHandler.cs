using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using fiskaltrust.Middleware.SCU.DE.FiskalyCertified.Exceptions;
using fiskaltrust.Middleware.SCU.DE.FiskalyCertified.Models;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace fiskaltrust.Middleware.SCU.DE.FiskalyCertified.Helpers
{
    public class AuthenticatedHttpClientHandler : HttpClientHandler
    {
        private const string ENDPOINT = "auth";
        internal static readonly TimeSpan InitialLoginBackoff = TimeSpan.FromSeconds(2);
        internal static readonly TimeSpan MaxLoginBackoff = TimeSpan.FromSeconds(30);

        private readonly FiskalySCUConfiguration _config;
        private readonly ILogger _logger;
        private readonly HttpClient _tokenClient;
        private readonly Func<DateTime> _utcNow;
        private readonly Func<TimeSpan, Task> _delay;
        private readonly SemaphoreSlim _loginLock = new SemaphoreSlim(1, 1);

        private volatile AccessToken _accessToken;
        private int _failedLogins;
        private DateTime _nextLoginAttemptAt = DateTime.MinValue;
        private Exception _lastLoginException;

        public AuthenticatedHttpClientHandler(FiskalySCUConfiguration config, ILogger logger)
            : this(config, logger, new HttpClientHandler { Proxy = ConfigurationHelper.CreateProxy(config) }, () => DateTime.UtcNow, Task.Delay)
        {
        }

        internal AuthenticatedHttpClientHandler(FiskalySCUConfiguration config, ILogger logger, HttpMessageHandler tokenHandler, Func<DateTime> utcNow, Func<TimeSpan, Task> delay)
        {
            _logger = logger;
            _config = config;
            _utcNow = utcNow;
            _delay = delay;

            // One client for all token requests of this SCU instance, so logins reuse the pooled connection instead of opening a new TCP/TLS connection each time.
            var url = _config.ApiEndpoint.EndsWith("/") ? _config.ApiEndpoint : $"{_config.ApiEndpoint}/";
            _tokenClient = new HttpClient(tokenHandler, disposeHandler: true)
            {
                BaseAddress = new Uri(url),
                Timeout = TimeSpan.FromMilliseconds(_config.FiskalyClientTimeout)
            };
        }

        internal async Task<string> GetToken(CancellationToken cancellationToken = default)
        {
            var token = _accessToken;
            if (IsValid(token))
            {
                return token.Value;
            }

            // Only one login at a time; concurrent callers wait for it and reuse its token.
            await _loginLock.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                token = _accessToken;
                if (IsValid(token))
                {
                    return token.Value;
                }

                if (_utcNow() < _nextLoginAttemptAt)
                {
                    throw new FiskalyException($"Could not get OAuth token from Fiskaly API: login failed {_failedLogins} time(s) in a row, next attempt not before {_nextLoginAttemptAt:O}.", _lastLoginException);
                }

                try
                {
                    token = await LoginAsync().ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    _failedLogins++;
                    var backoff = GetLoginBackoff(_failedLogins);
                    _nextLoginAttemptAt = _utcNow().Add(backoff);
                    _lastLoginException = ex;
                    _logger.LogWarning(ex, "Login to Fiskaly API failed {FailedLogins} time(s) in a row; pausing further logins for {BackoffMs} ms.", _failedLogins, backoff.TotalMilliseconds);
                    throw;
                }

                _logger.LogDebug("Logged in to Fiskaly API; access token valid until {ExpiresOn:O}.", token.ExpiresOn);
                _failedLogins = 0;
                _nextLoginAttemptAt = DateTime.MinValue;
                _lastLoginException = null;
                _accessToken = token;
                return token.Value;
            }
            finally
            {
                _loginLock.Release();
            }
        }

        internal static TimeSpan GetLoginBackoff(int failedLogins)
        {
            var factor = Math.Pow(2, Math.Min(failedLogins - 1, 16));
            var backoffMs = Math.Min(InitialLoginBackoff.TotalMilliseconds * factor, MaxLoginBackoff.TotalMilliseconds);
            return TimeSpan.FromMilliseconds(backoffMs);
        }

        private async Task<AccessToken> LoginAsync()
        {
            var requestContent = JsonConvert.SerializeObject(new TokenRequestDto
            {
                ApiKey = _config.ApiKey,
                ApiSecret = _config.ApiSecret
            });

            using var responseMessage = await PostAsync(requestContent).ConfigureAwait(false);
            var responseContent = await responseMessage.Content.ReadAsStringAsync().ConfigureAwait(false);
            var response = JsonConvert.DeserializeObject<TokenResponseDto>(responseContent);
            return new AccessToken(response.AccessToken, _utcNow().AddSeconds(response.ExpiresInSeconds * 0.9));
        }

        private async Task<HttpResponseMessage> PostAsync(string requestContent)
        {
            for (var attempt = 0; ; attempt++)
            {
                var responseMessage = await HttpClientWrapper.WrapCall(_tokenClient.PostAsync(ENDPOINT, new StringContent(requestContent, Encoding.UTF8, "application/json")), _config.FiskalyClientTimeout).ConfigureAwait(false);

                if (responseMessage.IsSuccessStatusCode)
                {
                    return responseMessage;
                }

                if ((int) responseMessage.StatusCode >= 500 && (int) responseMessage.StatusCode <= 599 && _config.RetriesOn5xxError > attempt)
                {
                    var retry = attempt + 1;
                    _logger.LogInformation($"HttpStatusCode {responseMessage.StatusCode} from Fiskaly retry {retry} from {_config.RetriesOn5xxError}");
                    responseMessage.Dispose();
                    await _delay(TimeSpan.FromMilliseconds(1000 * (retry + 1))).ConfigureAwait(false);
                    continue;
                }

                using (responseMessage)
                {
                    var content = await responseMessage.Content.ReadAsStringAsync().ConfigureAwait(false);
                    throw new FiskalyException($"Could not get OAuth token from Fiskaly API (Status code: {responseMessage.StatusCode}, Response: {content})");
                }
            }
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", await GetToken(cancellationToken).ConfigureAwait(false));
            return await HttpClientWrapper.WrapCall(base.SendAsync(request, cancellationToken), _config.FiskalyClientTimeout).ConfigureAwait(false);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _tokenClient.Dispose();
                _loginLock.Dispose();
            }
            base.Dispose(disposing);
        }

        private bool IsValid(AccessToken token) => token != null && token.ExpiresOn >= _utcNow();

        private sealed class AccessToken
        {
            public AccessToken(string value, DateTime expiresOn)
            {
                Value = value;
                ExpiresOn = expiresOn;
            }

            public string Value { get; }
            public DateTime ExpiresOn { get; }
        }
    }
}
