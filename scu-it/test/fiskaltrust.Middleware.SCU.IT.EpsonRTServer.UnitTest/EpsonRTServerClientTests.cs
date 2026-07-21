using System;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;

namespace fiskaltrust.Middleware.SCU.IT.EpsonRTServer.UnitTest
{
    public class EpsonRTServerClientTests
    {
        [Fact]
        public async Task CreateReceiptAsync_Should_Return_Accepted_Warning()
        {
            var client = CreateClient(-52, "till offline");

            var response = await client.CreateReceiptAsync("<createReceipt />");

            response.CodeAsInt.Should().Be(-52);
        }

        [Fact]
        public async Task CreateReceiptAsync_Should_Throw_For_Blocking_Rejection()
        {
            var client = CreateClient(-32, "refund or void not possible");

            Func<Task> act = () => client.CreateReceiptAsync("<createReceipt />");

            var exception = await act.Should().ThrowAsync<EpsonRTServerCommunicationException>();
            exception.Which.ResponseCode.Should().Be(-32);
        }

        private static EpsonRTServerClient CreateClient(int code, string status)
        {
            var configuration = new EpsonRTServerConfiguration { ServerUrl = "https://localhost" };
            var client = new EpsonRTServerClient(configuration, NullLogger<EpsonRTServerClient>.Instance);
            var httpClient = new HttpClient(new ResponseHandler(code, status))
            {
                BaseAddress = new Uri(configuration.ServerUrl)
            };
            typeof(EpsonRTServerClient).GetField("_httpClient", BindingFlags.NonPublic | BindingFlags.Instance)!
                .SetValue(client, httpClient);
            return client;
        }

        private sealed class ResponseHandler : HttpMessageHandler
        {
            private readonly int _code;
            private readonly string _status;

            public ResponseHandler(int code, string status)
            {
                _code = code;
                _status = status;
            }

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
                => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent($"<response success=\"false\" code=\"{_code}\" status=\"{_status}\" />")
                });
        }
    }
}
