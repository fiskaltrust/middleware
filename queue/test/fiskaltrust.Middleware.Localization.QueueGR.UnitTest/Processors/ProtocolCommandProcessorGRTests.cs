using fiskaltrust.Api.POS.Models.ifPOS.v2;
using fiskaltrust.Middleware.Localization.QueueGR.Processors;
using fiskaltrust.Middleware.Localization.v2;
using FluentAssertions;
using Xunit;
using fiskaltrust.Middleware.Localization.v2.Models.ifPOS.v2.Cases;
using Moq;
using Microsoft.Extensions.Logging;
using fiskaltrust.Middleware.Localization.QueueGR.GRSSCD;
using fiskaltrust.Middleware.Contracts.Repositories;

namespace fiskaltrust.Middleware.Localization.QueueGR.UnitTest.Processors;

public class ProtocolCommandProcessorGRTests
{
    [Theory]
    [InlineData(ReceiptCase.ProtocolUnspecified0x3000)]
    [InlineData(ReceiptCase.ProtocolTechnicalEvent0x3001)]
    [InlineData(ReceiptCase.ProtocolAccountingEvent0x3002)]
    [InlineData(ReceiptCase.InternalUsageMaterialConsumption0x3003)]
    [InlineData(ReceiptCase.Order0x3004)]
    [InlineData(ReceiptCase.CopyReceiptPrintExistingReceipt0x3010)]
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
        grSSCDMock.Setup(x => x.ProcessReceiptAsync(It.IsAny<ProcessRequest>(), null))
            .ReturnsAsync(new ProcessResponse
            {
                ReceiptResponse = receiptResponse,
            });

        var protocolCommandProcessorGR = new ProtocolCommandProcessorGR(grSSCDMock.Object, queueGR, scuGR, Mock.Of<IMiddlewareQueueItemRepository>());
        var receiptProcessor = new ReceiptProcessor(Mock.Of<ILogger<ReceiptProcessor>>(), null!, null!, null!, null!, protocolCommandProcessorGR);
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
        grSSCDMock.Setup(x => x.ProcessReceiptAsync(It.IsAny<ProcessRequest>(), null))
            .ReturnsAsync(new ProcessResponse
            {
                ReceiptResponse = receiptResponse,
            });

        var protocolCommandProcessorGR = new ProtocolCommandProcessorGR(grSSCDMock.Object, queueGR, scuGR, Mock.Of<IMiddlewareQueueItemRepository>());
        var receiptProcessor = new ReceiptProcessor(Mock.Of<ILogger<ReceiptProcessor>>(), null!, null!, null!, null!, protocolCommandProcessorGR);
        var result = await receiptProcessor.ProcessAsync(receiptRequest, receiptResponse, queue, queueItem);

        result.receiptResponse.Should().Be(receiptResponse);
        result.receiptResponse.ftState.Should().Be(0x4752_2000_EEEE_EEEE);
    }
}
