using System.Threading.Tasks;
using fiskaltrust.ifPOS.v2;
using fiskaltrust.Middleware.Localization.QueuePT.Processors;
using fiskaltrust.Middleware.Localization.v2.Interface;
using fiskaltrust.Middleware.Localization.v2;
using fiskaltrust.storage.V0;
using FluentAssertions;
using Xunit;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.ifPOS.v2.pt;
using Moq;
using fiskaltrust.Middleware.Contracts.Repositories;
using Microsoft.Extensions.Logging;

namespace fiskaltrust.Middleware.Localization.QueuePT.UnitTest.QueuePT.Processors;

public class ProtocolCommandProcessorPTTests
{
    private readonly ReceiptProcessor _sut = new(Mock.Of<ILogger<ReceiptProcessor>>(), null!, null!, null!, null!, new ProtocolCommandProcessorPT(Mock.Of<IPTSSCD>(), new ftQueuePT(), new(() => Task.FromResult(Mock.Of<IMiddlewareQueueItemRepository>()))));

    [Theory]
    [InlineData(ReceiptCase.Order0x3004)]
    public async Task ProcessReceiptAsync_ShouldCallScu_AndReturn(ReceiptCase receiptCase)
    {
        var queue = TestHelpers.CreateQueue();
        var queuePT = new ftQueuePT();
        var scuPT = new ftSignaturCreationUnitPT();
        var queueItem = TestHelpers.CreateQueueItem();
        var receiptRequest = new ReceiptRequest
        {
            ftReceiptCase = ((ReceiptCase) 0x5054_2000_0000_0000).WithCase(receiptCase),
            cbChargeItems = [],
            cbPayItems = [],
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

        var scuResponse = new ReceiptResponse
        {
            ftState = (State) 0x5054_2000_0000_0000,
            ftCashBoxIdentification = "cashBoxIdentification",
            ftQueueID = Guid.NewGuid(),
            ftQueueItemID = Guid.NewGuid(),
            ftQueueRow = 1,
            ftReceiptIdentification = "receiptIdentification",
            ftReceiptMoment = DateTime.UtcNow,
        };

        var grSSCDMock = new Mock<IPTSSCD>();
        grSSCDMock.Setup(x => x.ProcessReceiptAsync(It.IsAny<ProcessRequest>(), It.IsAny<string>()))
            .ReturnsAsync((new ProcessResponse
            {
                ReceiptResponse = receiptResponse,
            }, "jferer09ae9rf0oh3rlk4hj234o234ß92384ß023j4kl234lk2h3ö4lkh23ö4olkjh234"));
        var middlewareQueueItemRepositoryMock = new Mock<IMiddlewareQueueItemRepository>(MockBehavior.Strict);
        middlewareQueueItemRepositoryMock.Setup(x => x.GetAsync())
            .ReturnsAsync([]);
        var protocolCommandProcessorGR = new ProtocolCommandProcessorPT(grSSCDMock.Object, queuePT, new v2.Helpers.AsyncLazy<IMiddlewareQueueItemRepository>(() => Task.FromResult(middlewareQueueItemRepositoryMock.Object)));
        var receiptProcessor = new ReceiptProcessor(Mock.Of<ILogger<ReceiptProcessor>>(), null!, null!, null!, null!, protocolCommandProcessorGR);
        var result = await receiptProcessor.ProcessAsync(receiptRequest, receiptResponse, queue, queueItem);
        result.receiptResponse.ftState.Should().Be(0x5054_2000_0000_0000);
    }

    [Theory]
    [InlineData(ReceiptCase.ProtocolUnspecified0x3000)]
    [InlineData(ReceiptCase.ProtocolTechnicalEvent0x3001)]
    [InlineData(ReceiptCase.ProtocolAccountingEvent0x3002)]
    [InlineData(ReceiptCase.InternalUsageMaterialConsumption0x3003)]
    [InlineData(ReceiptCase.CopyReceiptPrintExistingReceipt0x3010)]
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
        var grSSCDMock = new Mock<IPTSSCD>();
        grSSCDMock.Setup(x => x.ProcessReceiptAsync(It.IsAny<ProcessRequest>(), It.IsAny<string>()))
            .ReturnsAsync((new ProcessResponse
            {
                ReceiptResponse = receiptResponse,
            }, ""));
        var middlewareQueueItemRepositoryMock = new Mock<IMiddlewareQueueItemRepository>(MockBehavior.Strict);

        var protocolCommandProcessorPT = new ProtocolCommandProcessorPT(grSSCDMock.Object, queuePT, new v2.Helpers.AsyncLazy<IMiddlewareQueueItemRepository>(() => Task.FromResult(middlewareQueueItemRepositoryMock.Object)));
        var receiptProcessor = new ReceiptProcessor(Mock.Of<ILogger<ReceiptProcessor>>(), null!, null!, null!, null!, protocolCommandProcessorPT);
        var result = await receiptProcessor.ProcessAsync(receiptRequest, receiptResponse, queue, queueItem);

        result.receiptResponse.Should().Be(receiptResponse);
        result.receiptResponse.ftState.Should().Be(0x5054_2000_EEEE_EEEE);
    }
}
