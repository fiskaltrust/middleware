using System.Threading.Tasks;
using fiskaltrust.ifPOS.v2;
using fiskaltrust.Middleware.Localization.QueueES.Processors;
using fiskaltrust.Middleware.Localization.v2.Interface;
using fiskaltrust.Middleware.Localization.v2;
using fiskaltrust.storage.V0;
using FluentAssertions;
using Xunit;
using fiskaltrust.ifPOS.v2.Cases;
using Moq;
using Castle.Core.Logging;
using Microsoft.Extensions.Logging;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.ifPOS.v2.es;

namespace fiskaltrust.Middleware.Localization.QueueES.UnitTest.Processors
{
    public class InvoiceCommandProcessorESTests
    {
        private ReceiptProcessor CreateSut(
            Guid? queueId = null,
            Mock<IESSSCD>? essscdMock = null,
            Mock<IConfigurationRepository>? configRepoMock = null,
            Mock<IMiddlewareQueueItemRepository>? queueItemRepoMock = null,
            Mock<IMiddlewareJournalESRepository>? journalRepoMock = null)
        {
            var actualQueueId = queueId ?? Guid.NewGuid();

            // Setup ESSSCD mock to return successful response
            if (essscdMock == null)
            {
                essscdMock = new Mock<IESSSCD>();
                essscdMock.Setup(x => x.ProcessReceiptAsync(It.IsAny<ProcessRequest>()))
                    .ReturnsAsync((ProcessRequest req) => new ProcessResponse
                    {
                        ReceiptResponse = req.ReceiptResponse
                    });
            }

            // Setup Configuration Repository mock to return valid ftQueueES
            if (configRepoMock == null)
            {
                configRepoMock = new Mock<IConfigurationRepository>();
                configRepoMock.Setup(x => x.GetQueueESAsync(actualQueueId))
                    .ReturnsAsync(new ftQueueES
                    {
                        ftQueueESId = actualQueueId,
                        ftSignaturCreationUnitESId = Guid.NewGuid(),
                        InvoiceSeries = "TEST",
                        InvoiceNumerator = 0,
                        SimplifiedInvoiceSeries = "STEST",
                        SimplifiedInvoiceNumerator = 0
                    });
                configRepoMock.Setup(x => x.InsertOrUpdateQueueESAsync(It.IsAny<ftQueueES>()))
                    .Returns(Task.CompletedTask);
            }

            // Setup Journal Repository mock
            if (journalRepoMock == null)
            {
                journalRepoMock = new Mock<IMiddlewareJournalESRepository>();
                journalRepoMock.Setup(x => x.InsertAsync(It.IsAny<ftJournalES>()))
                    .Returns(Task.CompletedTask);
            }

            var queueItemRepo = queueItemRepoMock?.Object ?? Mock.Of<IMiddlewareQueueItemRepository>();

            var invoiceProcessor = new InvoiceCommandProcessorES(
                Mock.Of<ILogger<InvoiceCommandProcessorES>>(),
                new(() => Task.FromResult(essscdMock.Object)),
                new(() => Task.FromResult(configRepoMock.Object)),
                new(() => Task.FromResult(queueItemRepo)),
                new(() => Task.FromResult(journalRepoMock.Object))
            );

            return new ReceiptProcessor(
                Mock.Of<ILogger<ReceiptProcessor>>(),
                null!,
                null!,
                null!,
                invoiceProcessor,
                null
            );
        }

        [Theory]
        [InlineData(ReceiptCase.InvoiceUnknown0x1000)]
        [InlineData(ReceiptCase.InvoiceB2C0x1001)]
        [InlineData(ReceiptCase.InvoiceB2B0x1002)]
        [InlineData(ReceiptCase.InvoiceB2G0x1003)]
        public async Task ProcessReceiptAsync_ShouldReturnEmptyList(ReceiptCase receiptCase)
        {
            var queue = TestHelpers.CreateQueue();
            var sut = CreateSut(queueId: queue.ftQueueId);
            var queueItem = TestHelpers.CreateQueueItem();
            var receiptRequest = new ReceiptRequest
            {
                ftReceiptCase = receiptCase.WithCountry("ES"),
                cbChargeItems = [],
                cbPayItems = []
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
            var result = await sut.ProcessAsync(receiptRequest, receiptResponse, queue, queueItem);

            result.receiptResponse.Should().Be(receiptResponse);
            result.receiptResponse.ftState.Should().Be(0x4752_2000_0000_0000);
        }

        [Fact]
        public async Task ProcessReceiptAsync_ShouldReturnError()
        {
            var queue = TestHelpers.CreateQueue();
            var sut = CreateSut(queueId: queue.ftQueueId);
            var queueItem = TestHelpers.CreateQueueItem();
            var receiptRequest = new ReceiptRequest
            {
                ftReceiptCase = ((ReceiptCase) 0).WithCountry("ES"),
                cbChargeItems = [],
                cbPayItems = []
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

            var result = await sut.ProcessAsync(receiptRequest, receiptResponse, queue, queueItem);
            result.receiptResponse.Should().Be(receiptResponse);
            result.receiptResponse.ftState.Should().Be(0x4752_2000_EEEE_EEEE);
        }
    }
}
