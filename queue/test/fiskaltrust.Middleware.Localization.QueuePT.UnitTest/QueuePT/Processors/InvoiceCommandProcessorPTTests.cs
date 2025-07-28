using fiskaltrust.ifPOS.v2;
using fiskaltrust.Middleware.Localization.QueuePT.Processors;
using fiskaltrust.Middleware.Localization.v2.Interface;
using fiskaltrust.Middleware.Localization.v2;
using FluentAssertions;
using Xunit;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.Middleware.Localization.QueuePT.PTSSCD;
using Moq;
using fiskaltrust.Middleware.Contracts.Repositories;
using Microsoft.Extensions.Logging;
using fiskaltrust.storage.V0;

namespace fiskaltrust.Middleware.Localization.QueuePT.UnitTest.QueuePT.Processors;

public class InvoiceCommandProcessorPTTests
{
    private readonly ReceiptProcessor _sut = new(Mock.Of<ILogger<ReceiptProcessor>>(), null!, null!, null!, new InvoiceCommandProcessorPT(Mock.Of<IPTSSCD>(), new ftQueuePT(), new(() => Task.FromResult(Mock.Of<IMiddlewareQueueItemRepository>()))), null!);

    [Theory]
    [InlineData(ReceiptCase.InvoiceUnknown0x1000)]
    [InlineData(ReceiptCase.InvoiceB2C0x1001, Skip = "broken")]
    [InlineData(ReceiptCase.InvoiceB2B0x1002)]
    [InlineData(ReceiptCase.InvoiceB2G0x1003)]
    public async Task ProcessReceiptAsync_ShouldReturnEmptyList(ReceiptCase receiptCase)
    {
        var queue = TestHelpers.CreateQueue();
        var queueItem = TestHelpers.CreateQueueItem();
        var receiptRequest = new ReceiptRequest
        {
            ftReceiptCase = receiptCase
        };
        var receiptResponse = new ReceiptResponse
        {
            ftState = (State) 0x5054_2000_0000_0000,
            ftCashBoxIdentification = "cashBoxIdentification",
            ftQueueID = Guid.NewGuid(),
            ftQueueItemID = Guid.NewGuid(),
            ftQueueRow = 1,
            ftReceiptIdentification = "receiptIdentification",
            ftReceiptMoment = DateTime.UtcNow,
        };
        var result = await _sut.ProcessAsync(receiptRequest, receiptResponse, queue, queueItem);

        result.receiptResponse.Should().Be(receiptResponse);
        result.receiptResponse.ftState.Should().Be(0x5054_2000_0000_0000);
    }

    [Fact(Skip = "broken")]
    public async Task ProcessReceiptAsync_ShouldReturnError()
    {
        var queue = TestHelpers.CreateQueue();
        var queueItem = TestHelpers.CreateQueueItem();
        var receiptRequest = new ReceiptRequest
        {
            ftReceiptCase = (ReceiptCase) 0
        };
        var receiptResponse = new ReceiptResponse
        {
            ftState = (State) 0x5054_2000_0000_0000,
            ftCashBoxIdentification = "cashBoxIdentification",
            ftQueueID = Guid.NewGuid(),
            ftQueueItemID = Guid.NewGuid(),
            ftQueueRow = 1,
            ftReceiptIdentification = "receiptIdentification",
            ftReceiptMoment = DateTime.UtcNow,
        };
        var result = await _sut.ProcessAsync(receiptRequest, receiptResponse, queue, queueItem);

        result.receiptResponse.Should().Be(receiptResponse);
        result.receiptResponse.ftState.Should().Be(0x5054_2000_EEEE_EEEE);
    }
}
