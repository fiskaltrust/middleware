using System;
using System.Threading.Tasks;
using fiskaltrust.Middleware.SCU.DE.FiskalyCertified.Helpers;
using fiskaltrust.Middleware.SCU.DE.FiskalyCertified.IntegrationTest;
using Xunit;
using FluentAssertions;
using fiskaltrust.Middleware.SCU.DE.FiskalyCertified.Exceptions;
using System.Net;
using Moq;
using System.Net.Http;
using Moq.Protected;
using System.Threading;

namespace fiskaltrust.Middleware.SCU.DE.FiskalyCertified.Services
{
    public class HttpClientWrapperTests
    {
        [Fact]
        public async Task PutAsync_BadGateway_RetryException()
        {
            var logger = new BadGatewayLogger();
            var config = new FiskalySCUConfiguration();

            var requestUri = "put/PutAsync_BadGateway_RetryException";
            var httpResponseMessage = new HttpResponseMessage()
            {
                StatusCode = HttpStatusCode.BadGateway,
                Content = new StringContent("PutAsync_BadGateway_RetryException")
            };
            var httpClient = MockHttpCient("PutAsync_BadGateway_RetryException");
            var httpClientWrapper = new HttpClientWrapper(config, logger, httpClient);
            var tss = Guid.NewGuid();
            var f = CallPutAsync(httpClientWrapper, requestUri, httpResponseMessage.Content);
            await f.Should().ThrowAsync<FiskalyException>().WithMessage($"Communication error ({HttpStatusCode.BadGateway}) while setting TSS metadata ({requestUri}). Response: PutAsync_BadGateway_RetryException").ConfigureAwait(false);
            CheckLogs(logger);
        }

        [Fact]
        public async Task GetAsync_BadGateway_RetryException()
        {
            var logger = new BadGatewayLogger();
            var config = new FiskalySCUConfiguration();
            var requestUri = "GET/GetAsync_BadGateway_RetryException";
            var httpClient = MockHttpCient("GetAsync_BadGateway_RetryException");
            var httpClientWrapper = new HttpClientWrapper(config, logger, httpClient);
            var tss = Guid.NewGuid();
            var f = CallGetAsync(httpClientWrapper, requestUri);
            await f.Should().ThrowAsync<FiskalyException>().WithMessage($"Communication error ({HttpStatusCode.BadGateway}) while getting TSS metadata ({requestUri}). Response: GetAsync_BadGateway_RetryException").ConfigureAwait(false);
            CheckLogs(logger);
        }

        [Fact]
        public async Task SendAsync_BadGateway_RetryException()
        {
            var logger = new BadGatewayLogger();
            var config = new FiskalySCUConfiguration();
            var httpClient = MockHttpCient("SendAsync_BadGateway_RetryException");
            var httpClientWrapper = new HttpClientWrapper(config, logger, httpClient);
            var tss = Guid.NewGuid();
            var requestUri = httpClient.BaseAddress.ToString() + "/SendAsync_BadGateway_RetryException";
            var request = new HttpRequestMessage()
            {
                RequestUri = new Uri(httpClient.BaseAddress.ToString()+ "/SendAsync_BadGateway_RetryException"),
                Content = new StringContent(requestUri)
            };
            var f = CallSendtAsync(httpClientWrapper, requestUri, "jsonPaylod_SendAsync_BadGateway_RetryException");
            await f.Should().ThrowAsync<FiskalyException>().WithMessage("Communication error (BadGateway) while setting TSS metadata (Method: PATCH, RequestUri: 'http://test.com//SendAsync_BadGateway_RetryException', Version: 1.1, Content: System.Net.Http.StringContent, Headers:\r\n{\r\n  Content-Type: application/json; charset=utf-8\r\n}). Response: SendAsync_BadGateway_RetryException").ConfigureAwait(false);
            CheckLogs(logger);
        }

        [Fact]
        public async Task PostAsync_BadGateway_RetryException()
        {
            var logger = new BadGatewayLogger();
            var config = new FiskalySCUConfiguration();
            var requestUri = "POST/PostAsync_BadGateway_RetryException";
            var httpClient = MockHttpCient("PostAsync_BadGateway_RetryException");
            var httpClientWrapper = new HttpClientWrapper(config, logger, httpClient);
            var tss = Guid.NewGuid();
            var f = CallPostAsync(httpClientWrapper, requestUri, new StringContent(requestUri));
            await f.Should().ThrowAsync<FiskalyException>().WithMessage($"Communication error ({HttpStatusCode.BadGateway}) while setting TSS metadata ({requestUri}). Response: PostAsync_BadGateway_RetryException").ConfigureAwait(false);
            CheckLogs(logger);
        }

        private static void CheckLogs(BadGatewayLogger logger)
        {
            var logs = logger.GetLog();
            logs.Should().HaveCount(2);
            logs[0].Should().Be("HttpStatusCode BadGateway from Fiskaly retry 1 from 2, DelayOnRetriesInMs: 100.");
            logs[1].Should().Be("HttpStatusCode BadGateway from Fiskaly retry 2 from 2, DelayOnRetriesInMs: 100.");
        }

        private HttpClient MockHttpCient(string content)
        {
            var handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
            handlerMock
               .Protected()
               // Setup the PROTECTED method to mock
               .Setup<Task<HttpResponseMessage>>(
                  "SendAsync",
                  ItExpr.IsAny<HttpRequestMessage>(),
                  ItExpr.IsAny<CancellationToken>()
               )
               // prepare the expected response of the mocked http call
               .ReturnsAsync(new HttpResponseMessage()
               {
                   StatusCode = HttpStatusCode.BadGateway,
                   Content = new StringContent(content)
               })
               .Verifiable();

            // use real http client with mocked handler here
            return new HttpClient(handlerMock.Object)
            {
                BaseAddress = new Uri("http://test.com/"),
            };
        }

        private Func<Task> CallPutAsync(HttpClientWrapper httpClientWrapper, string requestUri, HttpContent content)
        {
            return async () => await httpClientWrapper.PutAsync(requestUri, content).ConfigureAwait(false);
        }

        private Func<Task> CallGetAsync(HttpClientWrapper httpClientWrapper, string requestUri)
        {
            return async () => await httpClientWrapper.GetAsync(requestUri).ConfigureAwait(false);
        }

        private Func<Task> CallSendtAsync(HttpClientWrapper httpClientWrapper, string requestUri, string jsonPayload)
        {
            return async () => await httpClientWrapper.SendAsync(new HttpMethod("PATCH"), requestUri, jsonPayload).ConfigureAwait(false);
        }

        private Func<Task> CallPostAsync(HttpClientWrapper httpClientWrapper, string requestUri, HttpContent httpContent)
        {
            return async () => await httpClientWrapper.PostAsync(requestUri, httpContent).ConfigureAwait(false);
        }
    }
}
