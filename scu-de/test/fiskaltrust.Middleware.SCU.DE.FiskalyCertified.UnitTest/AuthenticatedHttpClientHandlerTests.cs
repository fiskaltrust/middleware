using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using fiskaltrust.Middleware.SCU.DE.FiskalyCertified.Exceptions;
using fiskaltrust.Middleware.SCU.DE.FiskalyCertified.Helpers;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace fiskaltrust.Middleware.SCU.DE.FiskalyCertified.UnitTest
{
    public class AuthenticatedHttpClientHandlerTests
    {
        private readonly FiskalySCUConfiguration _configuration = new FiskalySCUConfiguration
        {
            ApiEndpoint = "https://fiskaly.test/api/v2",
            ApiKey = "key",
            ApiSecret = "secret",
            RetriesOn5xxError = 2
        };
        private readonly List<TimeSpan> _delays = new List<TimeSpan>();
        private DateTime _now = new DateTime(2026, 10, 5, 12, 0, 0, DateTimeKind.Utc);

        private AuthenticatedHttpClientHandler CreateSut(TokenEndpointStub tokenEndpoint) =>
            new AuthenticatedHttpClientHandler(_configuration, Mock.Of<ILogger>(), tokenEndpoint, () => _now, d => { _delays.Add(d); return Task.CompletedTask; });

        [Fact]
        public async Task GetToken_CalledTwice_LogsInOnceAndReusesTokenClient()
        {
            var tokenEndpoint = new TokenEndpointStub(TokenEndpointStub.Token("token-1"));
            using var sut = CreateSut(tokenEndpoint);

            Assert.Equal("token-1", await sut.GetToken());
            Assert.Equal("token-1", await sut.GetToken());

            Assert.Equal(1, tokenEndpoint.Calls);
            Assert.False(tokenEndpoint.Disposed);
        }

        [Fact]
        public async Task GetToken_TokenExpired_LogsInAgainWithSameTokenClient()
        {
            var tokenEndpoint = new TokenEndpointStub(TokenEndpointStub.Token("token-1", expiresInSeconds: 100), TokenEndpointStub.Token("token-2"));
            using var sut = CreateSut(tokenEndpoint);

            Assert.Equal("token-1", await sut.GetToken());
            _now = _now.AddSeconds(91);
            Assert.Equal("token-2", await sut.GetToken());

            Assert.Equal(2, tokenEndpoint.Calls);
            Assert.False(tokenEndpoint.Disposed);
        }

        [Fact]
        public async Task GetToken_ConcurrentCallers_ShareASingleLogin()
        {
            var release = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            var tokenEndpoint = new TokenEndpointStub(async () =>
            {
                await release.Task.ConfigureAwait(false);
                return TokenEndpointStub.Token("token-1")();
            });
            using var sut = CreateSut(tokenEndpoint);

            var callers = Enumerable.Range(0, 10).Select(_ => Task.Run(() => sut.GetToken())).ToArray();
            await Task.Delay(100);
            release.SetResult(true);
            var tokens = await Task.WhenAll(callers);

            Assert.All(tokens, t => Assert.Equal("token-1", t));
            Assert.Equal(1, tokenEndpoint.Calls);
        }

        [Fact]
        public async Task GetToken_5xxThenSuccess_ReturnsTokenOfRetry()
        {
            var tokenEndpoint = new TokenEndpointStub(TokenEndpointStub.Status(HttpStatusCode.ServiceUnavailable), TokenEndpointStub.Token("token-1"));
            using var sut = CreateSut(tokenEndpoint);

            Assert.Equal("token-1", await sut.GetToken());

            Assert.Equal(2, tokenEndpoint.Calls);
            Assert.Equal(new[] { TimeSpan.FromSeconds(2) }, _delays);
        }

        [Fact]
        public async Task GetToken_5xxOnEveryAttempt_ThrowsAfterLastRetry()
        {
            var tokenEndpoint = new TokenEndpointStub(
                TokenEndpointStub.Status(HttpStatusCode.BadGateway),
                TokenEndpointStub.Status(HttpStatusCode.BadGateway),
                TokenEndpointStub.Status(HttpStatusCode.ServiceUnavailable));
            using var sut = CreateSut(tokenEndpoint);

            var ex = await Assert.ThrowsAsync<FiskalyException>(() => sut.GetToken());

            Assert.Contains("ServiceUnavailable", ex.Message);
            Assert.Equal(3, tokenEndpoint.Calls);
            Assert.Equal(new[] { TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(3) }, _delays);
        }

        [Fact]
        public async Task GetToken_4xx_ThrowsWithoutRetry()
        {
            var tokenEndpoint = new TokenEndpointStub(TokenEndpointStub.Status(HttpStatusCode.Unauthorized));
            using var sut = CreateSut(tokenEndpoint);

            await Assert.ThrowsAsync<FiskalyException>(() => sut.GetToken());

            Assert.Equal(1, tokenEndpoint.Calls);
            Assert.Empty(_delays);
        }

        [Fact]
        public async Task GetToken_AfterFailedLogin_FailsFastUntilBackoffElapsed()
        {
            var tokenEndpoint = new TokenEndpointStub(TokenEndpointStub.Timeout(), TokenEndpointStub.Token("token-1"));
            using var sut = CreateSut(tokenEndpoint);

            await Assert.ThrowsAsync<TaskCanceledException>(() => sut.GetToken());
            var failFast = await Assert.ThrowsAsync<FiskalyException>(() => sut.GetToken());
            Assert.IsType<TaskCanceledException>(failFast.InnerException);
            Assert.Equal(1, tokenEndpoint.Calls);

            _now = _now.Add(AuthenticatedHttpClientHandler.InitialLoginBackoff);
            Assert.Equal("token-1", await sut.GetToken());
            Assert.Equal(2, tokenEndpoint.Calls);
        }

        [Fact]
        public async Task GetToken_ConcurrentCallersDuringFailedLogin_LogInOnlyOnce()
        {
            var release = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            var tokenEndpoint = new TokenEndpointStub(async () =>
            {
                await release.Task.ConfigureAwait(false);
                throw new TaskCanceledException("The request was canceled due to the configured HttpClient.Timeout of 3 seconds elapsing.");
            });
            using var sut = CreateSut(tokenEndpoint);

            var callers = Enumerable.Range(0, 10).Select(_ => Task.Run(() => sut.GetToken())).ToArray();
            await Task.Delay(100);
            release.SetResult(true);
            foreach (var caller in callers)
            {
                await Assert.ThrowsAnyAsync<Exception>(() => caller);
            }

            Assert.Equal(1, tokenEndpoint.Calls);
        }

        [Theory]
        [InlineData(1, 2)]
        [InlineData(2, 4)]
        [InlineData(3, 8)]
        [InlineData(4, 16)]
        [InlineData(5, 30)]
        [InlineData(100, 30)]
        public void GetLoginBackoff_DoublesUpToMax(int failedLogins, int expectedSeconds)
        {
            Assert.Equal(TimeSpan.FromSeconds(expectedSeconds), AuthenticatedHttpClientHandler.GetLoginBackoff(failedLogins));
        }

        private class TokenEndpointStub : HttpMessageHandler
        {
            private readonly Queue<Func<Task<HttpResponseMessage>>> _responses;
            private int _calls;

            public TokenEndpointStub(params Func<HttpResponseMessage>[] responses)
                : this(responses.Select(r => (Func<Task<HttpResponseMessage>>) (() => Task.FromResult(r()))).ToArray())
            {
            }

            public TokenEndpointStub(params Func<Task<HttpResponseMessage>>[] responses)
            {
                _responses = new Queue<Func<Task<HttpResponseMessage>>>(responses);
            }

            public int Calls => _calls;
            public bool Disposed { get; private set; }

            public static Func<HttpResponseMessage> Token(string token, int expiresInSeconds = 3600) => () => new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent($"{{\"access_token\":\"{token}\",\"access_token_expires_in\":{expiresInSeconds}}}")
            };

            public static Func<HttpResponseMessage> Status(HttpStatusCode statusCode) => () => new HttpResponseMessage(statusCode)
            {
                Content = new StringContent("error")
            };

            public static Func<HttpResponseMessage> Timeout() => () => throw new TaskCanceledException("The request was canceled due to the configured HttpClient.Timeout of 3 seconds elapsing.");

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                Interlocked.Increment(ref _calls);
                Func<Task<HttpResponseMessage>> next;
                lock (_responses)
                {
                    next = _responses.Dequeue();
                }
                return next();
            }

            protected override void Dispose(bool disposing)
            {
                Disposed = true;
                base.Dispose(disposing);
            }
        }
    }
}
