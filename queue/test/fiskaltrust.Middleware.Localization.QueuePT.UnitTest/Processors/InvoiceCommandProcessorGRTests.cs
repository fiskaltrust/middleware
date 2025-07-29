using fiskaltrust.ifPOS.v2;
using fiskaltrust.Middleware.Localization.v2;
using FluentAssertions;
using Xunit;
using fiskaltrust.ifPOS.v2.Cases;
using Moq;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.storage.V0;
using System.Linq;
using System.Text.Json;
using fiskaltrust.Middleware.Localization.QueuePT.PTSSCD;
using fiskaltrust.Middleware.Localization.QueuePT.Processors;

namespace fiskaltrust.Middleware.Localization.QueuePT.UnitTest.Processors;

public class InvoiceCommandProcessorPTTests
{
    [Fact]
    public async Task InvoiceUnknown0x1000Async_ShouldReturnEmptyList()
    {
        var queue = TestHelpers.CreateQueue();
        var queuePT = new ftQueuePT();
        var scuPT = new ftSignaturCreationUnitPT();
        var queueItem = TestHelpers.CreateQueueItem();
        var receiptRequest = new ReceiptRequest
        {
            ftReceiptCase = ((ReceiptCase) 0x4752_2000_0000_0000).WithCase(ReceiptCase.InvoiceUnknown0x1000)
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

        var ptSSCDMock = new Mock<IPTSSCD>(MockBehavior.Strict);
        ptSSCDMock.Setup(x => x.ProcessReceiptAsync(It.IsAny<ProcessRequest>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync((new ProcessResponse
            {
                ReceiptResponse = receiptResponse,
            }, ""));
        var middlewareQueueItemRepositoryMock = new Mock<IMiddlewareQueueItemRepository>(MockBehavior.Strict);

        var invoiceCommandProcessor = new InvoiceCommandProcessorPT(ptSSCDMock.Object, queuePT, new(() => Task.FromResult(middlewareQueueItemRepositoryMock.Object)));
        var result = await invoiceCommandProcessor.InvoiceUnknown0x1000Async(new ProcessCommandRequest(queue, receiptRequest, receiptResponse));

        result.receiptResponse.Should().Be(receiptResponse);
        result.receiptResponse.ftState.Should().Be(0x4752_2000_0000_0000);
    }

    [Fact]
    public async Task InvoiceUnknown0x1000Async_RefundFlag_WithReferencedRefund_ShouldLoadQueueItem_ShouldReturnEmptyList()
    {
        var queue = TestHelpers.CreateQueue();
        var queuePT = new ftQueuePT();
        var scuPT = new ftSignaturCreationUnitPT();
        var queueItem = TestHelpers.CreateQueueItem();
        queueItem.request = JsonSerializer.Serialize(new ReceiptRequest
        {
            ftReceiptCase = ((ReceiptCase) 0x4752_2000_0000_0000).WithCase(ReceiptCase.InvoiceUnknown0x1000),
            cbTerminalID = "terminalId",
            cbPreviousReceiptReference = "previousReceiptReference",
        });
        queueItem.response = JsonSerializer.Serialize(new ReceiptResponse
        {
            cbReceiptReference = "previousReceiptReference",
            ftQueueItemID = queueItem.ftQueueItemId,
            ftQueueID = queueItem.ftQueueId,
            ftState = (State) 0x4752_2000_0000_0000,
            ftCashBoxIdentification = "cashBoxIdentification",
            ftQueueRow = 1,
            ftReceiptIdentification = "receiptIdentification",
            ftReceiptMoment = DateTime.UtcNow,
        });

        var receiptRequest = new ReceiptRequest
        {
            ftReceiptCase = ((ReceiptCase) 0x4752_2000_0000_0000).WithCase(ReceiptCase.InvoiceUnknown0x1000),
            cbPreviousReceiptReference = "previousReceiptReference",
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

        var ptSSCDMock = new Mock<IPTSSCD>(MockBehavior.Strict);
        ptSSCDMock.Setup(x => x.ProcessReceiptAsync(It.IsAny<ProcessRequest>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync((new ProcessResponse
            {
                ReceiptResponse = receiptResponse,
            }, ""));
        var middlewareQueueItemRepositoryMock = new Mock<IMiddlewareQueueItemRepository>();
        middlewareQueueItemRepositoryMock.Setup(x => x.GetByReceiptReferenceAsync(receiptRequest.cbPreviousReceiptReference.SingleValue, receiptRequest.cbTerminalID))
            .Returns(new List<ftQueueItem> { queueItem }.ToAsyncEnumerable());

        var invoiceCommandProcessor = new InvoiceCommandProcessorPT(ptSSCDMock.Object, queuePT, new(() => Task.FromResult(middlewareQueueItemRepositoryMock.Object)));
        var result = await invoiceCommandProcessor.InvoiceUnknown0x1000Async(new ProcessCommandRequest(queue, receiptRequest, receiptResponse));

        result.receiptResponse.Should().Be(receiptResponse);
        result.receiptResponse.ftState.Should().Be(0x4752_2000_0000_0000);
    }

    [Fact]
    public async Task InvoiceUnknown0x1000Async_RefundFlag_WithUnreferencedRefund_ShouldNotLoadQueueItem_ShouldReturnEmptyList()
    {
        var queue = TestHelpers.CreateQueue();
        var queuePT = new ftQueuePT();
        var scuPT = new ftSignaturCreationUnitPT();
        var queueItem = TestHelpers.CreateQueueItem();
        var receiptRequest = new ReceiptRequest
        {
            ftReceiptCase = ((ReceiptCase) 0x4752_2000_0000_0000).WithCase(ReceiptCase.InvoiceUnknown0x1000)
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

        var ptSSCDMock = new Mock<IPTSSCD>(MockBehavior.Strict);
        ptSSCDMock.Setup(x => x.ProcessReceiptAsync(It.IsAny<ProcessRequest>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync((new ProcessResponse
            {
                ReceiptResponse = receiptResponse,
            }, ""));
        var middlewareQueueItemRepositoryMock = new Mock<IMiddlewareQueueItemRepository>(MockBehavior.Strict);

        var invoiceCommandProcessor = new InvoiceCommandProcessorPT(ptSSCDMock.Object, queuePT, new(() => Task.FromResult(middlewareQueueItemRepositoryMock.Object)));
        var result = await invoiceCommandProcessor.InvoiceUnknown0x1000Async(new ProcessCommandRequest(queue, receiptRequest, receiptResponse));

        result.receiptResponse.Should().Be(receiptResponse);
        result.receiptResponse.ftState.Should().Be(0x4752_2000_0000_0000);
    }

    [Fact]
    public async Task InvoiceB2C0x1001Async_ShouldReturnEmptyList()
    {
        var queue = TestHelpers.CreateQueue();
        var queuePT = new ftQueuePT();
        var scuPT = new ftSignaturCreationUnitPT();
        var queueItem = TestHelpers.CreateQueueItem();
        var receiptRequest = new ReceiptRequest
        {
            ftReceiptCase = ((ReceiptCase) 0x4752_2000_0000_0000).WithCase(ReceiptCase.InvoiceB2C0x1001)
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

        var ptSSCDMock = new Mock<IPTSSCD>(MockBehavior.Strict);
        ptSSCDMock.Setup(x => x.ProcessReceiptAsync(It.IsAny<ProcessRequest>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync((new ProcessResponse
            {
                ReceiptResponse = receiptResponse,
            }, ""));
        var middlewareQueueItemRepositoryMock = new Mock<IMiddlewareQueueItemRepository>(MockBehavior.Strict);

        var invoiceCommandProcessor = new InvoiceCommandProcessorPT(ptSSCDMock.Object, queuePT, new(() => Task.FromResult(middlewareQueueItemRepositoryMock.Object)));
        var result = await invoiceCommandProcessor.InvoiceB2C0x1001Async(new ProcessCommandRequest(queue, receiptRequest, receiptResponse));

        result.receiptResponse.Should().Be(receiptResponse);
        result.receiptResponse.ftState.Should().Be(0x4752_2000_0000_0000);
    }

    [Fact]
    public async Task InvoiceB2B0x1002Async_ShouldReturnEmptyList()
    {
        var queue = TestHelpers.CreateQueue();
        var queuePT = new ftQueuePT();
        var scuPT = new ftSignaturCreationUnitPT();
        var queueItem = TestHelpers.CreateQueueItem();
        var receiptRequest = new ReceiptRequest
        {
            ftReceiptCase = ((ReceiptCase) 0x4752_2000_0000_0000).WithCase(ReceiptCase.InvoiceB2B0x1002)
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

        var ptSSCDMock = new Mock<IPTSSCD>(MockBehavior.Strict);
        ptSSCDMock.Setup(x => x.ProcessReceiptAsync(It.IsAny<ProcessRequest>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync((new ProcessResponse
            {
                ReceiptResponse = receiptResponse,
            }, ""));
        var middlewareQueueItemRepositoryMock = new Mock<IMiddlewareQueueItemRepository>(MockBehavior.Strict);

        var invoiceCommandProcessor = new InvoiceCommandProcessorPT(ptSSCDMock.Object, queuePT, new(() => Task.FromResult(middlewareQueueItemRepositoryMock.Object)));
        var result = await invoiceCommandProcessor.InvoiceB2B0x1002Async(new ProcessCommandRequest(queue, receiptRequest, receiptResponse));

        result.receiptResponse.Should().Be(receiptResponse);
        result.receiptResponse.ftState.Should().Be(0x4752_2000_0000_0000);
    }

    [Fact]
    public async Task InvoiceB2G0x1003Async_ShouldReturnEmptyList()
    {
        var queue = TestHelpers.CreateQueue();
        var queuePT = new ftQueuePT();
        var scuPT = new ftSignaturCreationUnitPT();
        var queueItem = TestHelpers.CreateQueueItem();
        var receiptRequest = new ReceiptRequest
        {
            ftReceiptCase = ((ReceiptCase) 0x4752_2000_0000_0000).WithCase(ReceiptCase.InvoiceB2G0x1003)
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

        var ptSSCDMock = new Mock<IPTSSCD>(MockBehavior.Strict);
        ptSSCDMock.Setup(x => x.ProcessReceiptAsync(It.IsAny<ProcessRequest>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync((new ProcessResponse
            {
                ReceiptResponse = receiptResponse,
            }, ""));
        var middlewareQueueItemRepositoryMock = new Mock<IMiddlewareQueueItemRepository>(MockBehavior.Strict);

        var invoiceCommandProcessor = new InvoiceCommandProcessorPT(ptSSCDMock.Object, queuePT, new(() => Task.FromResult(middlewareQueueItemRepositoryMock.Object)));
        var result = await invoiceCommandProcessor.InvoiceB2G0x1003Async(new ProcessCommandRequest(queue, receiptRequest, receiptResponse));

        result.receiptResponse.Should().Be(receiptResponse);
        result.receiptResponse.ftState.Should().Be(0x4752_2000_0000_0000);
    }
}
