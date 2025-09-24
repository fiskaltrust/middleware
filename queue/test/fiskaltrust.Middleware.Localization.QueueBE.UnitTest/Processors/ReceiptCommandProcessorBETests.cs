using fiskaltrust.ifPOS.v2;
using fiskaltrust.Middleware.Localization.QueueBE.Processors;
using fiskaltrust.Middleware.Localization.QueueBE.BESSCD;
using fiskaltrust.Middleware.Localization.v2;
using fiskaltrust.Middleware.Localization.v2.Helpers;
using fiskaltrust.Middleware.Contracts.Repositories;
using FluentAssertions;
using Xunit;
using fiskaltrust.ifPOS.v2.Cases;
using Microsoft.Extensions.Logging;
using Moq;

namespace fiskaltrust.Middleware.Localization.QueueBE.UnitTest.Processors;

public class ReceiptCommandProcessorBETests
{
    private readonly Mock<IBESSCD> _mockBESSCD = new();
    private readonly ReceiptProcessor _sut;

    public ReceiptCommandProcessorBETests()
    {
        _mockBESSCD.Setup(x => x.ProcessReceiptAsync(It.IsAny<ProcessRequest>(), It.IsAny<List<(ReceiptRequest, ReceiptResponse)>>()))
                   .ReturnsAsync((ProcessRequest req, List<(ReceiptRequest, ReceiptResponse)> refs) => 
                       new ProcessResponse { ReceiptResponse = req.ReceiptResponse });

        _sut = new(Mock.Of<ILogger<ReceiptProcessor>>(), null!, 
                   new ReceiptCommandProcessorBE(_mockBESSCD.Object, new AsyncLazy<IMiddlewareQueueItemRepository>(() => Task.FromResult(Mock.Of<IMiddlewareQueueItemRepository>()))), 
                   null!, null!, null!);
    }

    [Theory]
    [InlineData(ReceiptCase.UnknownReceipt0x0000)]
    [InlineData(ReceiptCase.PointOfSaleReceipt0x0001)]
    [InlineData(ReceiptCase.PaymentTransfer0x0002)]
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
        _mockBESSCD.Verify(x => x.ProcessReceiptAsync(It.IsAny<ProcessRequest>(), It.IsAny<List<(ReceiptRequest, ReceiptResponse)>>()), Times.Once);
    }

    [Theory]
    [InlineData(ReceiptCase.PointOfSaleReceiptWithoutObligation0x0003)]
    [InlineData(ReceiptCase.ECommerce0x0004)]
    [InlineData(ReceiptCase.DeliveryNote0x0005)]
    public async Task ProcessReceiptAsync_NotSupported_ShouldThrowException(ReceiptCase receiptCase)
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

        var act = async () => await _sut.ProcessAsync(receiptRequest, receiptResponse, queue, queueItem);

        await act.Should().ThrowAsync<NotSupportedException>()
            .WithMessage("*QueueBE*");
    }
}