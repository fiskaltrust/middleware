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

            var response = await httpClientWrapper.PutAsync(requestUri, httpResponseMessage.Content);
            response.StatusCode.Should().Be(HttpStatusCode.BadGateway);
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

            var response = await httpClientWrapper.GetAsync(requestUri);
            response.StatusCode.Should().Be(HttpStatusCode.BadGateway);
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
                RequestUri = new Uri(httpClient.BaseAddress.ToString() + "/SendAsync_BadGateway_RetryException"),
                Content = new StringContent(requestUri)
            };

            var response = await httpClientWrapper.SendAsync(new HttpMethod("PATCH"), requestUri, "jsonPaylod_SendAsync_BadGateway_RetryException");
            response.StatusCode.Should().Be(HttpStatusCode.BadGateway);
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
            var response = await httpClientWrapper.PostAsync(requestUri, new StringContent(requestUri));
            response.StatusCode.Should().Be(HttpStatusCode.BadGateway);
            CheckLogs(logger);
        }

        private static void CheckLogs(BadGatewayLogger logger)
        {
            var logs = logger.GetLog();
            logs.Should().HaveCount(5);
            logs[0].Should().Be("HttpStatusCode BadGateway from Fiskaly retry 1 from 5, DelayOnRetriesInMs: 1000.");
            logs[1].Should().Be("HttpStatusCode BadGateway from Fiskaly retry 2 from 5, DelayOnRetriesInMs: 1000.");
            logs[2].Should().Be("HttpStatusCode BadGateway from Fiskaly retry 3 from 5, DelayOnRetriesInMs: 1000.");
            logs[3].Should().Be("HttpStatusCode BadGateway from Fiskaly retry 4 from 5, DelayOnRetriesInMs: 1000.");
            logs[4].Should().Be("HttpStatusCode BadGateway from Fiskaly retry 5 from 5, DelayOnRetriesInMs: 1000.");

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
    }
}
