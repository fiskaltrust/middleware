using System;
using System.IO;
using System.Threading.Tasks;
using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.pt;
using fiskaltrust.Middleware.Localization.QueuePT.Processors;
using fiskaltrust.Middleware.Localization.v2.Interface;
using fiskaltrust.Middleware.Localization.v2;
using fiskaltrust.Middleware.Storage;
using fiskaltrust.storage.V0;
using FluentAssertions;
using FluentAssertions.Execution;
using Moq;
using Xunit;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.Middleware.Contracts.Repositories;
using Microsoft.Extensions.Logging;

namespace fiskaltrust.Middleware.Localization.QueuePT.UnitTest.QueuePT.Processors;

public class ReceiptCommandProcessorPTTests
{
    private readonly ReceiptProcessor _sut = new(Mock.Of<ILogger<ReceiptProcessor>>(), null!, new ReceiptCommandProcessorPT(Mock.Of<IPTSSCD>(), new ftQueuePT(), new(() => Task.FromResult(Mock.Of<IMiddlewareQueueItemRepository>()))), null!, null!, null!);

    [Theory]
    [InlineData(ReceiptCase.PointOfSaleReceiptWithoutObligation0x0003)]
    [InlineData(ReceiptCase.ECommerce0x0004)]
    [InlineData(ReceiptCase.DeliveryNote0x0005)]
    public async Task ProcessReceiptAsync_NoOp_Should_ReturnResponse(ReceiptCase receiptCase)
    {
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
        var result = await _sut.ProcessAsync(receiptRequest, receiptResponse, new ftQueue { }, new ftQueueItem { });
        result.receiptResponse.ftSignatures.Should().BeEmpty();
        result.receiptResponse.Should().Be(receiptResponse);
        result.receiptResponse.ftState.Should().Be(0x5054_2000_0000_0000);
    }

    [Fact]
    public async Task ProcessReceiptAsync_ShouldReturnError()
    {
        var queue = TestHelpers.CreateQueue();
        var queuePT = new ftQueuePT();
        var scuPT = new ftSignaturCreationUnitPT();
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