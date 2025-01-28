using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.Middleware.Localization.QueueIT.v2;
using fiskaltrust.storage.V0;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace fiskaltrust.Middleware.Localization.QueueIT.UnitTest.v2;

public class ReceiptCommandProcessorITTests
{
    [Fact]
    public async Task PointOfSaleReceipt0x0001Async_Void_ShouldReturn_EEEE_Tag_IfReceiptReference_IsNotAvailable()
    {
        var cbPreviousReceiptReference = Guid.NewGuid().ToString();

        var queueItemRepositoryMock = new Mock<IMiddlewareQueueItemRepository>(MockBehavior.Strict);
        queueItemRepositoryMock.Setup(x => x.GetByReceiptReferenceAsync(cbPreviousReceiptReference, "")).Returns(new List<ftQueueItem> { }.ToAsyncEnumerable());

        var itSSCDProvider = Mock.Of<IITSSCDProvider>(MockBehavior.Strict);
        var journalITRepository = Mock.Of<IJournalITRepository>(MockBehavior.Strict);
        var logger = Mock.Of<ILogger<ReceiptCommandProcessorIT>>();

        var request = new ProcessCommandRequest(new ftQueue(), new ftQueueIT(), new ReceiptRequest
        {
            ftReceiptCase = 0x4954200000000000 | (long) ReceiptCases.PointOfSaleReceipt0x0001 | 0x0000_0000_0004_0000,
            cbPreviousReceiptReference = cbPreviousReceiptReference
        }, new ReceiptResponse(), new ftQueueItem());
        var processor = new ReceiptCommandProcessorIT(itSSCDProvider, journalITRepository, queueItemRepositoryMock.Object);

        var result = await processor.PointOfSaleReceipt0x0001Async(request);

       (result.receiptResponse.ftState & 0xFFFF_FFFF).Should().Be(0xEEEE_EEEE);
    }

    [Fact]
    public async Task PointOfSaleReceipt0x0001Async_Refund_ShouldReturn_EEEE_Tag_IfReceiptReference_IsNotAvailable()
    {
        var cbPreviousReceiptReference = Guid.NewGuid().ToString();

        var queueItemRepositoryMock = new Mock<IMiddlewareQueueItemRepository>(MockBehavior.Strict);
        queueItemRepositoryMock.Setup(x => x.GetByReceiptReferenceAsync(cbPreviousReceiptReference, "")).Returns(new List<ftQueueItem> { }.ToAsyncEnumerable());

        var itSSCDProvider = Mock.Of<IITSSCDProvider>(MockBehavior.Strict);
        var journalITRepository = Mock.Of<IJournalITRepository>(MockBehavior.Strict);
        var logger = Mock.Of<ILogger<ReceiptCommandProcessorIT>>();

        var request = new ProcessCommandRequest(new ftQueue(), new ftQueueIT(), new ReceiptRequest
        {
            ftReceiptCase = 0x4954200000000000 | (long) ReceiptCases.PointOfSaleReceipt0x0001 | 0x0000_0000_0100_0000,
            cbPreviousReceiptReference = cbPreviousReceiptReference
        }, new ReceiptResponse(), new ftQueueItem());
        var processor = new ReceiptCommandProcessorIT(itSSCDProvider, journalITRepository, queueItemRepositoryMock.Object);

        var result = await processor.PointOfSaleReceipt0x0001Async(request);

        (result.receiptResponse.ftState & 0xFFFF_FFFF).Should().Be(0xEEEE_EEEE);
    }
}