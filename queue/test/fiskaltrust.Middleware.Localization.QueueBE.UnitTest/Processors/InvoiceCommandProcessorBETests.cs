using fiskaltrust.ifPOS.v2;
using fiskaltrust.Middleware.Localization.QueueBE.Processors;
using fiskaltrust.Middleware.Localization.v2;
using fiskaltrust.Middleware.Localization.v2.Helpers;
using fiskaltrust.Middleware.Contracts.Repositories;
using FluentAssertions;
using Xunit;
using fiskaltrust.ifPOS.v2.Cases;
using Microsoft.Extensions.Logging;
using Moq;
using fiskaltrust.ifPOS.v2.be;

namespace fiskaltrust.Middleware.Localization.QueueBE.UnitTest.Processors;

public class InvoiceCommandProcessorBETests
{
    private readonly Mock<IBESSCD> _mockBESSCD = new();
    private readonly ReceiptProcessor _sut;

    public InvoiceCommandProcessorBETests()
    {
        _mockBESSCD.Setup(x => x.ProcessReceiptAsync(It.IsAny<ProcessRequest>()))
                   .ReturnsAsync((ProcessRequest req, List<(ReceiptRequest, ReceiptResponse)> refs) => 
                       new ProcessResponse { ReceiptResponse = req.ReceiptResponse });

        _sut = new(Mock.Of<ILogger<ReceiptProcessor>>(), null!, null!, null!, 
                   new InvoiceCommandProcessorBE(_mockBESSCD.Object, new AsyncLazy<IMiddlewareQueueItemRepository>(() => Task.FromResult(Mock.Of<IMiddlewareQueueItemRepository>()))), null!);
    }

    [Theory]
    [InlineData(ReceiptCase.InvoiceUnknown0x1000)]
    [InlineData(ReceiptCase.InvoiceB2C0x1001)]
    [InlineData(ReceiptCase.InvoiceB2B0x1002)]
    [InlineData(ReceiptCase.InvoiceB2G0x1003)]
    public async Task ProcessReceiptAsync_ShouldCallBESSCD(ReceiptCase receiptCase)
    {
        var queue = TestHelpers.CreateQueue();
        var queueItem = TestHelpers.CreateQueueItem();
        var receiptRequest = new ReceiptRequest
        {
            ftReceiptCase = receiptCase
        };
        var receiptResponse = new ReceiptResponse
        {
            ftState = (State) 0x4245_2000_0000_0000, // BE state
            ftCashBoxIdentification = "cashBoxIdentification",
            ftQueueID = Guid.NewGuid(),
            ftQueueItemID = Guid.NewGuid(),
            ftQueueRow = 1,
            ftReceiptIdentification = "receiptIdentification",
            ftReceiptMoment = DateTime.UtcNow,
        };

        var result = await _sut.ProcessAsync(receiptRequest, receiptResponse, queue, queueItem);

        result.receiptResponse.Should().Be(receiptResponse);
        result.receiptResponse.ftState.Should().Be(0x4245_2000_0000_0000);
    }
}