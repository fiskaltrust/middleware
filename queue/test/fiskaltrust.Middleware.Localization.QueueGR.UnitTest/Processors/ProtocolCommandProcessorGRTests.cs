using fiskaltrust.ifPOS.v2;
using fiskaltrust.Middleware.Localization.QueueGR.Models;
using fiskaltrust.Middleware.Localization.QueueGR.Processors;
using fiskaltrust.Middleware.Localization.v2;
using fiskaltrust.Middleware.Localization.v2.Helpers;
using fiskaltrust.Middleware.Localization.v2.Interface;
using FluentAssertions;
using Xunit;
using fiskaltrust.ifPOS.v2.Cases;
using Moq;
using Microsoft.Extensions.Logging;
using fiskaltrust.ifPOS.v2.gr;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.storage.V0;
using fiskaltrust.Middleware.Localization.v2.Storage;
using fiskaltrust.Middleware.Localization.v2.Validation;

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
        var queueStorageProviderMock = new Mock<IQueueStorageProvider>();
        queueStorageProviderMock.Setup(x => x.GetReceiptReferencesIfNecessaryAsync(It.IsAny<ProcessCommandRequest>()))
            .ReturnsAsync(new List<(ReceiptRequest, ReceiptResponse)>());
        var protocolCommandProcessorGR = new ProtocolCommandProcessorGR(grSSCDMock.Object, queueStorageProviderMock.Object, TestHelpers.CreateConfigurationRepositoryStub(), Mock.Of<ILogger>());
        var receiptProcessor = new ReceiptProcessor(Mock.Of<ILogger<ReceiptProcessor>>(), Mock.Of<IMarketValidator>(), null!, null!, null!, null!, protocolCommandProcessorGR);
        var result = await receiptProcessor.ProcessAsync(receiptRequest, receiptResponse, queue, queueItem);

        result.receiptResponse.Should().Be(scuResponse);
        result.receiptResponse.ftState.Should().Be(0x4752_2000_0000_0000);
    }

    [Fact]
    public async Task Order0x3004_ConsumesInvoiceCounter_WhenInvoiceIsFiled()
    {
        // Orders are submitted to myDATA via SendInvoices like receipts, so they draw
        // from the same invoice counter. The mark gate decides whether an aa was
        // consumed: a filed order (mark present) advances the counter.
        var queue = TestHelpers.CreateQueue();
        var queueGR = new ftQueueGR
        {
            ftQueueGRId = queue.ftQueueId,
            CashBoxIdentification = "CB-A",
            InvoiceSeries = "CB-A",
            InvoiceNumerator = 41,
        };
        var configRepo = new Mock<IConfigurationRepository>();
        configRepo.Setup(x => x.GetQueueGRAsync(It.IsAny<Guid>())).ReturnsAsync(queueGR);
        configRepo.Setup(x => x.InsertOrUpdateQueueGRAsync(It.IsAny<ftQueueGR>())).Returns(Task.CompletedTask);

        var capturedIdentifications = new List<string>();
        var grSSCDMock = new Mock<IGRSSCD>();
        grSSCDMock.Setup(x => x.ProcessReceiptAsync(It.IsAny<ProcessRequest>(), It.IsAny<List<(ReceiptRequest, ReceiptResponse)>>()))
            .ReturnsAsync((ProcessRequest req, List<(ReceiptRequest, ReceiptResponse)> _) =>
            {
                capturedIdentifications.Add(req.ReceiptResponse.ftReceiptIdentification!);
                req.ReceiptResponse.ftState = req.ReceiptResponse.ftState.WithState(State.Success);
                req.ReceiptResponse.AddSignatureItem(new SignatureItem
                {
                    Caption = "invoiceMark",
                    Data = "400001",
                    ftSignatureFormat = SignatureFormat.Text,
                    ftSignatureType = SignatureTypeGR.Mark.As<SignatureType>(),
                });
                return new ProcessResponse { ReceiptResponse = req.ReceiptResponse };
            });
        var queueStorageProviderMock = new Mock<IQueueStorageProvider>();
        queueStorageProviderMock.Setup(x => x.GetReceiptReferencesIfNecessaryAsync(It.IsAny<ProcessCommandRequest>()))
            .ReturnsAsync(new List<(ReceiptRequest, ReceiptResponse)>());

        var processor = new ProtocolCommandProcessorGR(
            grSSCDMock.Object,
            queueStorageProviderMock.Object,
            new AsyncLazy<IConfigurationRepository>(() => Task.FromResult(configRepo.Object)),
            Mock.Of<ILogger>());

        var receiptRequest = new ReceiptRequest
        {
            ftReceiptCase = ((ReceiptCase) 0x4752_2000_0000_0000).WithCase(ReceiptCase.Order0x3004),
            cbReceiptMoment = DateTime.UtcNow,
        };
        var receiptResponse = new ReceiptResponse
        {
            ftState = (State) 0x4752_2000_0000_0000,
            ftCashBoxIdentification = "CB-A",
            ftQueueID = queue.ftQueueId,
            ftQueueItemID = Guid.NewGuid(),
            ftQueueRow = 1,
            ftReceiptIdentification = "ft1#",
            ftReceiptMoment = DateTime.UtcNow,
        };

        await processor.Order0x3004Async(new ProcessCommandRequest(queue, receiptRequest, receiptResponse));

        capturedIdentifications.Should().Equal("ft1#CB-A-42");
        queueGR.InvoiceNumerator.Should().Be(42);
        queueGR.LastInvoiceMark.Should().Be(400001);
        configRepo.Verify(x => x.InsertOrUpdateQueueGRAsync(queueGR), Times.Once);
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

        var queueStorageProviderMock = new Mock<IQueueStorageProvider>();

        var grSSCDMock = new Mock<IGRSSCD>(MockBehavior.Strict);
        var protocolCommandProcessorGR = new ProtocolCommandProcessorGR(grSSCDMock.Object, queueStorageProviderMock.Object, TestHelpers.CreateConfigurationRepositoryStub(), Mock.Of<ILogger>());
        var receiptProcessor = new ReceiptProcessor(Mock.Of<ILogger<ReceiptProcessor>>(), Mock.Of<IMarketValidator>(), null!, null!, null!, null!, protocolCommandProcessorGR);
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
        var queueStorageProviderMock = new Mock<IQueueStorageProvider>();

        var grSSCDMock = new Mock<IGRSSCD>();
        grSSCDMock.Setup(x => x.ProcessReceiptAsync(It.IsAny<ProcessRequest>(), new List<(ReceiptRequest, ReceiptResponse)>()))
            .ReturnsAsync(new ProcessResponse
            {
                ReceiptResponse = receiptResponse,
            });

        var protocolCommandProcessorGR = new ProtocolCommandProcessorGR(grSSCDMock.Object, queueStorageProviderMock.Object, TestHelpers.CreateConfigurationRepositoryStub(), Mock.Of<ILogger>());
        var receiptProcessor = new ReceiptProcessor(Mock.Of<ILogger<ReceiptProcessor>>(), Mock.Of<IMarketValidator>(), null!, null!, null!, null!, protocolCommandProcessorGR);
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
        var queueStorageProviderMock = new Mock<IQueueStorageProvider>();
        var grSSCDMock = new Mock<IGRSSCD>();
        grSSCDMock.Setup(x => x.ProcessReceiptAsync(It.IsAny<ProcessRequest>(), new List<(ReceiptRequest, ReceiptResponse)>()))
            .ReturnsAsync(new ProcessResponse
            {
                ReceiptResponse = receiptResponse,
            });

        var protocolCommandProcessorGR = new ProtocolCommandProcessorGR(grSSCDMock.Object, queueStorageProviderMock.Object, TestHelpers.CreateConfigurationRepositoryStub(), Mock.Of<ILogger>());
        var receiptProcessor = new ReceiptProcessor(Mock.Of<ILogger<ReceiptProcessor>>(), Mock.Of<IMarketValidator>(), null!, null!, null!, null!, protocolCommandProcessorGR);
        var result = await receiptProcessor.ProcessAsync(receiptRequest, receiptResponse, queue, queueItem);

        result.receiptResponse.Should().Be(receiptResponse);
        result.receiptResponse.ftState.Should().Be(0x4752_2000_EEEE_EEEE);
    }
}
