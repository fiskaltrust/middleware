using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using fiskaltrust.ifPOS.v1.errors;

namespace fiskaltrust.Middleware.SCU.IT.EpsonRTPrinter.UnitTest
{
    public class LocalEpsonFpMateClientTests
    {
        [Fact]
        public async Task SendCommandAsync_WhenHttpClientTimesOut_ThrowsEpsonNoResponseException()
        {
            var timeoutException = new TaskCanceledException("The request timed out.");
            var sut = CreateClient(new StubHttpMessageHandler((_, _) => throw timeoutException));

            var exception = await Assert.ThrowsAsync<EpsonNoResponseException>(() => sut.SendCommandAsync("<command />"));

            Assert.IsAssignableFrom<TaskCanceledException>(exception);
            Assert.Same(timeoutException, exception.InnerException);
        }

        [Fact]
        public async Task SendCommandAsync_WhenEpsonReturnsNonSuccessStatus_DoesNotClassifyAsNoResponse()
        {
            var response = new HttpResponseMessage(HttpStatusCode.Unauthorized)
            {
                Content = new StringContent("Unauthorized request")
            };
            var sut = CreateClient(new StubHttpMessageHandler((_, _) => Task.FromResult(response)));

            var exception = await Assert.ThrowsAsync<HttpRequestException>(() => sut.SendCommandAsync("<command />"));

            Assert.IsNotType<EpsonNoResponseException>(exception);
            Assert.Contains("Unauthorized", exception.Message);
        }

        [Fact]
        public void ExceptionInfo_WhenEpsonDoesNotRespond_ClassifiesAsConnectionError()
        {
            var response = Helpers.ExceptionInfo(new EpsonNoResponseException("No response."));

            Assert.Equal(SSCDErrorType.Connection, response.SSCDErrorInfo.Type);
        }

        private static LocalEpsonFpMateClient CreateClient(HttpMessageHandler handler) =>
            new(new EpsonRTPrinterSCUConfiguration
            {
                DeviceUrl = "http://localhost",
                ClientTimeoutMs = 1000,
                ServerTimeoutMs = 500
            }, handler);

        private sealed class StubHttpMessageHandler : HttpMessageHandler
        {
            private readonly Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> _sendAsync;

            public StubHttpMessageHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> sendAsync)
            {
                _sendAsync = sendAsync;
            }

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
                _sendAsync(request, cancellationToken);
        }
    }
}
