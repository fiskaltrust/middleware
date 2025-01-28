using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.ifPOS.v1.it;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.Middleware.Localization.QueueIT.Constants;
using fiskaltrust.Middleware.Localization.QueueIT.Helpers;
using fiskaltrust.Middleware.Localization.QueueIT.v2;
using fiskaltrust.storage.V0;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Newtonsoft.Json;
using Xunit;

#pragma warning disable

namespace fiskaltrust.Middleware.Localization.QueueIT.UnitTest.v2;

public class ProtocolCommandProcessorITTests
{
    [Fact]
    public async Task CopyReceiptPrintExistingReceipt0x3010Async_ShouldReturn_EEEE_Tag_IfReceiptReference_IsNotAvailable()
    {
        var cbPreviousReceiptReference = Guid.NewGuid().ToString();

        var queueItemRepositoryMock = new Mock<IMiddlewareQueueItemRepository>();
        queueItemRepositoryMock.Setup(x => x.GetByReceiptReferenceAsync(cbPreviousReceiptReference, null)).Returns(new List<ftQueueItem> { }.ToAsyncEnumerable());

        var itSSCDProvider = Mock.Of<IITSSCDProvider>(MockBehavior.Strict);
        var journalITRepository = Mock.Of<IJournalITRepository>(MockBehavior.Strict);
        var logger = Mock.Of<ILogger<ProtocolCommandProcessorIT>>();

        var request = new ProcessCommandRequest(new ftQueue(), new ftQueueIT(), new ReceiptRequest
        {
            cbPreviousReceiptReference = cbPreviousReceiptReference
        }, new ReceiptResponse(), new ftQueueItem());
        var processor = new ProtocolCommandProcessorIT(itSSCDProvider, journalITRepository, queueItemRepositoryMock.Object, logger);

        var result = await processor.CopyReceiptPrintExistingReceipt0x3010Async(request);

       (result.receiptResponse.ftState & 0xFFFF_FFFF).Should().Be(0xEEEE_EEEE);
    }
}