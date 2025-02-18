using System.Threading.Tasks;
using AutoFixture;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.Middleware.Contracts.Interfaces;
using FluentAssertions;
using Grpc.Core;
using Moq;
using ProtoBuf.Grpc.Client;
using ProtoBuf.Grpc.Server;
using Xunit;

namespace fiskaltrust.Middleware.Queue.AcceptanceTest
{
    public class IPOSTests
    {
        private readonly string _host = "localhost";
        private readonly int _port = 20044;

        public Server StartHost(IPOS posInstance)
        {
            var server = new Server
            {
                Ports = { new ServerPort(_host, _port, ServerCredentials.Insecure) }
            };
            server.Services.AddCodeFirst(posInstance);
            server.Start();
            return server;
        }

        public IPOS GetClient()
        {
            var channel = new Channel(_host, _port, ChannelCredentials.Insecure);
            return channel.CreateGrpcService<IPOS>();
        }

        [Fact]
        public async Task EchoAsync_ShouldReturnMessage()
        {
            var fixture = new Fixture();
            var echoRequest = fixture.Create<EchoRequest>();

            var server = StartHost(new Queue(null, null, new Contracts.Models.MiddlewareConfiguration()));
            var client = GetClient();

            var echoResponse = await client.EchoAsync(echoRequest);
            echoResponse.Message.Should().Be(echoRequest.Message);

            await server.ShutdownAsync();
        }

        [Fact]
        public async Task SignAsync_Should_CallSignProcessor_AndSucceedWithExpectedResponse()
        {
            var fixture = new Fixture();
            var receiptRequest = fixture.Create<ReceiptRequest>();
            var receiptResponse = fixture.Create<ReceiptResponse>();

            var mock = new Mock<ISignProcessor>(MockBehavior.Strict);
            mock.Setup(x => x.ProcessAsync(It.Is<ReceiptRequest>(y => Matches(y, receiptRequest)))).ReturnsAsync(receiptResponse);

            var server = StartHost(new Queue(mock.Object, null, new Contracts.Models.MiddlewareConfiguration()));
            var client = GetClient();

            var signResponse = await client.SignAsync(receiptRequest);

            signResponse.Should().BeEquivalentTo(signResponse);

            await server.ShutdownAsync();
        }

        private bool Matches<T>(T actualRequest, T expectedRequest)
        {
            actualRequest.Should().BeEquivalentTo(expectedRequest);
            return true;
        }
    }
}
