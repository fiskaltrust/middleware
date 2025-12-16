using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.gr;
using fiskaltrust.Middleware.Localization.QueueGR.Processors;
using fiskaltrust.Middleware.Localization.v2;
using FluentAssertions;
using Moq;
using Xunit;
using fiskaltrust.ifPOS.v2.Cases;
using Microsoft.Extensions.Logging;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.storage.V0;
using fiskaltrust.Middleware.Localization.QueueGR.Models.Cases;

namespace fiskaltrust.Middleware.Localization.QueueGR.UnitTest.Processors;

public class ReceiptCommandProcessorGRTests
{
    [Theory]
    [InlineData(ReceiptCase.PointOfSaleReceipt0x0001)]
    [InlineData(ReceiptCase.PaymentTransfer0x0002)]
    public async Task ProcessReceiptAsync_ShouldReturnEmptyList(ReceiptCase receiptCase)
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

        var receiptCommandProcessor = new ReceiptCommandProcessorGR(grSSCDMock.Object, new(() => Task.FromResult(Mock.Of<IMiddlewareQueueItemRepository>())));
        var receiptProcessor = new ReceiptProcessor(Mock.Of<ILogger<ReceiptProcessor>>(), null!, receiptCommandProcessor, null!, null!, null!);
        var result = await receiptProcessor.ProcessAsync(receiptRequest, receiptResponse, queue, queueItem);

        result.receiptResponse.Should().Be(scuResponse);
        result.receiptResponse.ftState.Should().Be(0x4752_2000_0000_0000);
    }

    [Theory]
    [InlineData(ReceiptCase.PointOfSaleReceiptWithoutObligation0x0003)]
    [InlineData(ReceiptCase.ECommerce0x0004)]
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
        var grSSCDMock = new Mock<IGRSSCD>(MockBehavior.Strict);
        var receiptCommandProcessor = new ReceiptCommandProcessorGR(grSSCDMock.Object, new(() => Task.FromResult(Mock.Of<IMiddlewareQueueItemRepository>())));
        var receiptProcessor = new ReceiptProcessor(Mock.Of<ILogger<ReceiptProcessor>>(), null!, receiptCommandProcessor, null!, null!, null!);
        var result = await receiptProcessor.ProcessAsync(receiptRequest, receiptResponse, queue, queueItem);

        result.receiptResponse.Should().Be(receiptResponse);
        result.receiptResponse.ftState.Should().Be(0x4752_2000_EEEE_EEEE);
        result.receiptResponse.ftSignatures.Should().HaveCount(1);
        result.receiptResponse.ftSignatures[0].Data.Should().EndWith(" is not supported in the QueueGR implementation.");
    }

    [Fact]
    public async Task ProcessReceiptAsync_DeliveryNoteWithoutTransportInformation_ShouldReturnError()
    {
        var queue = TestHelpers.CreateQueue();
        var queueGR = new ftQueueGR();
        var scuGR = new ftSignaturCreationUnitGR();
        var queueItem = TestHelpers.CreateQueueItem();
        var receiptRequest = new ReceiptRequest
        {
            ftReceiptCase = ((ReceiptCase) 0x4752_2000_0000_0000).WithCase(ReceiptCase.DeliveryNote0x0005)
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
        var receiptCommandProcessor = new ReceiptCommandProcessorGR(grSSCDMock.Object, new(() => Task.FromResult(Mock.Of<IMiddlewareQueueItemRepository>())));
        var receiptProcessor = new ReceiptProcessor(Mock.Of<ILogger<ReceiptProcessor>>(), null!, receiptCommandProcessor, null!, null!, null!);
        var result = await receiptProcessor.ProcessAsync(receiptRequest, receiptResponse, queue, queueItem);

        result.receiptResponse.Should().Be(receiptResponse);
        result.receiptResponse.ftState.Should().Be(0x4752_2000_EEEE_EEEE);
        result.receiptResponse.ftSignatures.Should().HaveCount(1);
        result.receiptResponse.ftSignatures[0].Data.Should().Contain("DeliveryNote");
        result.receiptResponse.ftSignatures[0].Data.Should().EndWith(" is not supported in the QueueGR implementation.");
    }

    [Fact]
    public async Task ProcessReceiptAsync_DeliveryNoteWithTransportInformation_ShouldForwardToSCU()
    {
        var queue = TestHelpers.CreateQueue();
        var queueGR = new ftQueueGR();
        var scuGR = new ftSignaturCreationUnitGR();
        var queueItem = TestHelpers.CreateQueueItem();
        var receiptRequest = new ReceiptRequest
        {
            // DeliveryNote with HasTransportInformation flag using extension method
            ftReceiptCase = ((ReceiptCase) 0x4752_2000_0000_0000)
                .WithCase(ReceiptCase.DeliveryNote0x0005)
                .WithFlag(Models.Cases.ReceiptCaseFlags.HasTransportInformation)
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
        grSSCDMock.Setup(x => x.ProcessReceiptAsync(It.IsAny<ProcessRequest>(), It.IsAny<List<(ReceiptRequest, ReceiptResponse)>>()))
            .ReturnsAsync(new ProcessResponse
            {
                ReceiptResponse = scuResponse,
            });

        var receiptCommandProcessor = new ReceiptCommandProcessorGR(grSSCDMock.Object, new(() => Task.FromResult(Mock.Of<IMiddlewareQueueItemRepository>())));
        var receiptProcessor = new ReceiptProcessor(Mock.Of<ILogger<ReceiptProcessor>>(), null!, receiptCommandProcessor, null!, null!, null!);
        var result = await receiptProcessor.ProcessAsync(receiptRequest, receiptResponse, queue, queueItem);

        // Verify that SCU was called
        grSSCDMock.Verify(x => x.ProcessReceiptAsync(It.IsAny<ProcessRequest>(), It.IsAny<List<(ReceiptRequest, ReceiptResponse)>>()), Times.Once);

        // Verify response from SCU is returned
        result.receiptResponse.Should().Be(scuResponse);
        result.receiptResponse.ftState.Should().Be(0x4752_2000_0000_0000);
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
        var receiptCommandProcessor = new ReceiptCommandProcessorGR(grSSCDMock.Object, new(() => Task.FromResult(Mock.Of<IMiddlewareQueueItemRepository>())));
        var receiptProcessor = new ReceiptProcessor(Mock.Of<ILogger<ReceiptProcessor>>(), null!, receiptCommandProcessor, null!, null!, null!);
        var result = await receiptProcessor.ProcessAsync(receiptRequest, receiptResponse, queue, queueItem);

        result.receiptResponse.Should().Be(receiptResponse);
        result.receiptResponse.ftState.Should().Be(0x4752_2000_EEEE_EEEE);
    }
}
