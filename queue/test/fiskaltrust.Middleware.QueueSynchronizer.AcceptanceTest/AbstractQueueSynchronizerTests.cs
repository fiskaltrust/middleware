using System.Threading.Tasks;
using Xunit;
using FluentAssertions;
using fiskaltrust.ifPOS.v1;
using Moq;
using System.Collections.Generic;
using fiskaltrust.Middleware.Contracts.Interfaces;

namespace fiskaltrust.Middleware.QueueSynchronizer.AcceptanceTest
{
    public abstract class AbstractQueueSynchronizerTests
    {
        public abstract ISignProcessor CreateDecorator(ISignProcessor signProcessor);

        [Fact]
        public async Task ProcessAsync_Should_Return_Expected_Response_From_SignProcessor()
        {
            var request = new ReceiptRequest();
            var expectedResponse = new ReceiptResponse();

            var signProcessorMock = new Mock<ISignProcessor>();
            signProcessorMock.Setup(x => x.ProcessAsync(request)).ReturnsAsync(expectedResponse);

            var sut = CreateDecorator(signProcessorMock.Object);
            var actualResponse = await sut.ProcessAsync(request);

            actualResponse.Should().Be(expectedResponse);
        }

        [Fact]
        public async Task ProcessAsync_Should_Return_Expected_Response_For_SingleThreadedUsage()
        {
            var signProcessorMock = new Mock<ISignProcessor>();

            var sut = CreateDecorator(signProcessorMock.Object);

            var tasks = new List<Task<ReceiptResponse>>();
            var expectedResponses = new List<ReceiptResponse>();

            for (var i = 0; i < 10; i++)
            {
                var request = new ReceiptRequest() { cbReceiptReference = i.ToString() };
                var response = new ReceiptResponse { cbReceiptReference = i.ToString() };
                expectedResponses.Add(response);
                signProcessorMock.Setup(x => x.ProcessAsync(request)).ReturnsAsync(response);
                tasks.Add(sut.ProcessAsync(request));
            }

            var actualResponses = await Task.WhenAll(tasks);
            actualResponses.Should().BeEquivalentTo(expectedResponses);
        }
    }
}
