using fiskaltrust.ifPOS.v2;
using fiskaltrust.Middleware.Localization.QueueGR.Processors;
using fiskaltrust.Middleware.Localization.v2;
using FluentAssertions;
using Xunit;
using fiskaltrust.ifPOS.v2.Cases;
using Moq;
using Microsoft.Extensions.Logging;
using fiskaltrust.ifPOS.v2.gr;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.storage.V0;

namespace fiskaltrust.Middleware.Localization.QueueGR.UnitTest.Processors;

public class ProtocolCommandProcessorGRTests
{
    [Theory]
    [InlineData(ReceiptCase.Order0x3004)]
    public async Task ProcessReceiptAsync_ShouldCallScu_AndReturn(ReceiptCase receiptCase)
    {
        var queue = TestHelpers.CreateQueue();
        var queueGR = new ftQueueGR();
        var scuGR = new ftSignaturCreationUnitGR();
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

        var scuResponse = new ReceiptResponse
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
        grSSCDMock.Setup(x => x.ProcessReceiptAsync(It.IsAny<ProcessRequest>(), new List<(ReceiptRequest, ReceiptResponse)>()))
            .ReturnsAsync(new ProcessResponse
            {
                ReceiptResponse = scuResponse,
            });

        var protocolCommandProcessorGR = new ProtocolCommandProcessorGR(grSSCDMock.Object);
        var receiptProcessor = new ReceiptProcessor(Mock.Of<ILogger<ReceiptProcessor>>(), null!, null!, null!, null!, protocolCommandProcessorGR);
        var result = await receiptProcessor.ProcessAsync(receiptRequest, receiptResponse, queue, queueItem);

        result.receiptResponse.Should().Be(scuResponse);
        result.receiptResponse.ftState.Should().Be(0x4752_2000_0000_0000);
    }

    [Theory]
    [InlineData(ReceiptCase.ProtocolUnspecified0x3000)]
    [InlineData(ReceiptCase.ProtocolTechnicalEvent0x3001)]
    [InlineData(ReceiptCase.ProtocolAccountingEvent0x3002)]
    [InlineData(ReceiptCase.CopyReceiptPrintExistingReceipt0x3010)]
    public async Task ProcessReceiptAsync_NoOp_Should_ReturnResponse(ReceiptCase receiptCase)
    {
        var queue = TestHelpers.CreateQueue();
        var queueGR = new ftQueueGR();
        var scuGR = new ftSignaturCreationUnitGR();
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

        var grSSCDMock = new Mock<IGRSSCD>(MockBehavior.Strict);
        var protocolCommandProcessorGR = new ProtocolCommandProcessorGR(grSSCDMock.Object);
        var receiptProcessor = new ReceiptProcessor(Mock.Of<ILogger<ReceiptProcessor>>(), null!, null!, null!, null!, protocolCommandProcessorGR);
        var result = await receiptProcessor.ProcessAsync(receiptRequest, receiptResponse, queue, queueItem);

        result.receiptResponse.Should().Be(receiptResponse);
        result.receiptResponse.ftState.Should().Be(0x4752_2000_0000_0000);
    }

    [Theory]
    [InlineData(ReceiptCase.InternalUsageMaterialConsumption0x3003)]
    public async Task ProcessReceiptAsync_ForNotSupportedOperations_ShouldReturnError(ReceiptCase receiptCase)
    {
        var queue = TestHelpers.CreateQueue();
        var queueGR = new ftQueueGR();
        var scuGR = new ftSignaturCreationUnitGR();
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
        grSSCDMock.Setup(x => x.ProcessReceiptAsync(It.IsAny<ProcessRequest>(), new List<(ReceiptRequest, ReceiptResponse)>()))
            .ReturnsAsync(new ProcessResponse
            {
                ReceiptResponse = receiptResponse,
            });

        var protocolCommandProcessorGR = new ProtocolCommandProcessorGR(grSSCDMock.Object);
        var receiptProcessor = new ReceiptProcessor(Mock.Of<ILogger<ReceiptProcessor>>(), null!, null!, null!, null!, protocolCommandProcessorGR);
        var result = await receiptProcessor.ProcessAsync(receiptRequest, receiptResponse, queue, queueItem);

        result.receiptResponse.Should().Be(receiptResponse);
        result.receiptResponse.ftState.Should().Be(0x4752_2000_EEEE_EEEE);
        result.receiptResponse.ftSignatures.Should().HaveCount(1);
        result.receiptResponse.ftSignatures[0].Data.Should().EndWith(" is not supported in the QueueGR implementation.");
    }

    [Fact]
    public async Task ProcessReceiptAsync_ShouldReturnError()
    {
        var queue = TestHelpers.CreateQueue();
        var queueGR = new ftQueueGR();
        var scuGR = new ftSignaturCreationUnitGR();
        var queueItem = TestHelpers.CreateQueueItem();
        var receiptRequest = new ReceiptRequest
        {
            ftReceiptCase = (ReceiptCase) 0
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
        grSSCDMock.Setup(x => x.ProcessReceiptAsync(It.IsAny<ProcessRequest>(), new List<(ReceiptRequest, ReceiptResponse)>()))
            .ReturnsAsync(new ProcessResponse
            {
                ReceiptResponse = receiptResponse,
            });

        var protocolCommandProcessorGR = new ProtocolCommandProcessorGR(grSSCDMock.Object);
        var receiptProcessor = new ReceiptProcessor(Mock.Of<ILogger<ReceiptProcessor>>(), null!, null!, null!, null!, protocolCommandProcessorGR);
        var result = await receiptProcessor.ProcessAsync(receiptRequest, receiptResponse, queue, queueItem);

        result.receiptResponse.Should().Be(receiptResponse);
        result.receiptResponse.ftState.Should().Be(0x4752_2000_EEEE_EEEE);
    }
}
