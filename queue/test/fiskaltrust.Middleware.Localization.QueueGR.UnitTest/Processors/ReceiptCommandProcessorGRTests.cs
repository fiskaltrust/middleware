using fiskaltrust.Api.POS.Models.ifPOS.v2;
using fiskaltrust.Middleware.Localization.QueueGR.GRSSCD;
using fiskaltrust.Middleware.Localization.QueueGR.Processors;
using fiskaltrust.Middleware.Localization.v2;
using FluentAssertions;
using Moq;
using Xunit;
using fiskaltrust.Middleware.Localization.v2.Models.ifPOS.v2.Cases;
using Microsoft.Extensions.Logging;

namespace fiskaltrust.Middleware.Localization.QueueGR.UnitTest.Processors;

public class ReceiptCommandProcessorGRTests
{
    [Theory]
    [InlineData(ReceiptCase.PointOfSaleReceipt0x0001)]
    [InlineData(ReceiptCase.PaymentTransfer0x0002)]
    [InlineData(ReceiptCase.PointOfSaleReceiptWithoutObligation0x0003)]
    [InlineData(ReceiptCase.ECommerce0x0004)]
    [InlineData(ReceiptCase.Protocol0x0005)]
    public async Task ProcessReceiptAsync_ShouldReturnEmptyList(ReceiptCase receiptCase)
    {
        var queue = TestHelpers.CreateQueue();
        var queueGR = new Storage.GR.ftQueueGR();
        var scuGR = new Storage.GR.ftSignaturCreationUnitGR();
        var queueItem = TestHelpers.CreateQueueItem();
        var receiptRequest = new ReceiptRequest
        {
            ftReceiptCase = ((ReceiptCase) 0x4752_2000_0000_0000).WithCase(receiptCase)
        };
        var receiptResponse = new ReceiptResponse
        {
            ftState = (State) 0x4752_2000_0000_0000,
            ftCashBoxIdentification = "cashBoxIdentification",
            ftQueueID = Guid.NewGuid(),
            ftQueueItemID = Guid.NewGuid(),
            ftQueueRow = 1,
            ftReceiptIdentification = "receiptIdentification",
            ftReceiptMoment = DateTime.UtcNow,
        };
        var grSSCDMock = new Mock<IGRSSCD>();
        grSSCDMock.Setup(x => x.ProcessReceiptAsync(It.IsAny<ProcessRequest>()))
            .ReturnsAsync(new ProcessResponse
            {
                ReceiptResponse = receiptResponse,
            });

        var receiptCommandProcessor = new ReceiptCommandProcessorGR(grSSCDMock.Object, queueGR, scuGR);
        var receiptProcessor = new ReceiptProcessor(Mock.Of<ILogger<ReceiptProcessor>>(), null!, receiptCommandProcessor, null!, null!, null!);
        var result = await receiptProcessor.ProcessAsync(receiptRequest, receiptResponse, queue, queueItem);

        result.receiptResponse.Should().Be(receiptResponse);
        result.receiptResponse.ftState.Should().Be(0x4752_2000_0000_0000);
    }

    [Fact]
    public async Task ProcessReceiptAsync_ShouldReturnError()
    {
        var queue = TestHelpers.CreateQueue();
        var queueGR = new Storage.GR.ftQueueGR();
        var scuGR = new Storage.GR.ftSignaturCreationUnitGR();
        var queueItem = TestHelpers.CreateQueueItem();
        var receiptRequest = new ReceiptRequest
        {
            ftReceiptCase = (ReceiptCase) (-1)
        };
        var receiptResponse = new ReceiptResponse
        {
            ftState = (State) 0x4752_2000_0000_0000,
            ftCashBoxIdentification = "cashBoxIdentification",
            ftQueueID = Guid.NewGuid(),
            ftQueueItemID = Guid.NewGuid(),
            ftQueueRow = 1,
            ftReceiptIdentification = "receiptIdentification",
            ftReceiptMoment = DateTime.UtcNow,
        };
        var grSSCDMock = new Mock<IGRSSCD>();
        var receiptCommandProcessor = new ReceiptCommandProcessorGR(grSSCDMock.Object, queueGR, scuGR);
        var receiptProcessor = new ReceiptProcessor(Mock.Of<ILogger<ReceiptProcessor>>(), null!, receiptCommandProcessor, null!, null!, null!);
        var result = await receiptProcessor.ProcessAsync(receiptRequest, receiptResponse, queue, queueItem);

        result.receiptResponse.Should().Be(receiptResponse);
        result.receiptResponse.ftState.Should().Be(0x4752_2000_EEEE_EEEE);
    }
}
