using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.Middleware.Contracts;
using fiskaltrust.Middleware.Contracts.Models;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.storage.V0;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Newtonsoft.Json;
using Xunit;

namespace fiskaltrust.Middleware.Queue.AcceptanceTest
{

    public class JournalProcessorUnitTests
    {
        [Fact]
        public async Task GetExportWithCorrectCountryCodeAsync_Should_NotThrow()
        {
            var loggergMock = new Mock<ILogger<JournalProcessor>>();
            var configMock = new Mock<IConfigurationRepository>(MockBehavior.Strict);
            var queueItemRepositoryMock = new Mock<IMiddlewareQueueItemRepository>(MockBehavior.Strict);
            var receiptJournalRepositoryMock = new Mock<IMiddlewareReceiptJournalRepository>(MockBehavior.Strict);
            var actionJournalRepositoryMock = new Mock<IMiddlewareActionJournalRepository>(MockBehavior.Strict);
            var journalATRepositoryMock = new Mock<IMiddlewareRepository<ftJournalAT>>(MockBehavior.Strict);
            var journalDERepositoryMock = new Mock<IMiddlewareRepository<ftJournalDE>>(MockBehavior.Strict);
            var journalFRRepositoryMock = new Mock<IMiddlewareJournalFRRepository>(MockBehavior.Strict);
            var journalMERepositoryMock = new Mock<IMiddlewareRepository<ftJournalME>>(MockBehavior.Strict);
            var marketSpecificJournalProcessorMock = new Mock<IMarketSpecificJournalProcessor>(MockBehavior.Strict);
            var queueId = Guid.NewGuid();
            var cashboxId = Guid.NewGuid();
            var receiptRequestMode = 0;
            var receiptHash = "MyHash";

            var queue = new ftQueue
            {
                ftCashBoxId = cashboxId,
                ftQueueId = queueId,
                CountryCode = "DE"
            };

            var queueItem = new ftQueueItem()
            {
                ftQueueItemId = Guid.NewGuid(),
            };
            var request = new JournalRequest()
            {
                ftJournalType = 0x4445000000000000
            };

            var receiptJournal = new ftReceiptJournal()
            {
                ftQueueId = queueId,
                ftQueueItemId = queueItem.ftQueueItemId,
                ftReceiptNumber = queue.ftReceiptNumerator,
                ftReceiptHash = receiptHash
            };

            var configuration = new MiddlewareConfiguration
            {
                QueueId = queueId,
                CashBoxId = cashboxId,
                ReceiptRequestMode = receiptRequestMode,
                Configuration = new() { { "init_ftQueue", JsonConvert.SerializeObject(new List<ftQueue>() { queue }) } }
            };


            marketSpecificJournalProcessorMock.Setup(x => x.ProcessAsync(request)).Returns(new List<JournalResponse>() { new JournalResponse { Chunk = new() } }.ToAsyncEnumerable());
            receiptJournalRepositoryMock.Setup(x => x.InsertAsync(It.IsAny<ftReceiptJournal>())).Returns(Task.CompletedTask);
            configMock.Setup(x => x.InsertOrUpdateQueueAsync(queue)).Returns(Task.CompletedTask);

            var sut = new JournalProcessor(configMock.Object, queueItemRepositoryMock.Object, receiptJournalRepositoryMock.Object, actionJournalRepositoryMock.Object, journalATRepositoryMock.Object, journalDERepositoryMock.Object, journalFRRepositoryMock.Object, journalMERepositoryMock.Object, marketSpecificJournalProcessorMock.Object, loggergMock.Object, configuration);


            await foreach(var chunk in sut.ProcessAsync(request))
            {

            }
        }

        [Fact]
        public void GetExportWithIorrectCountryCode_Should_Throw()
        {
            var loggergMock = new Mock<ILogger<JournalProcessor>>();
            var configMock = new Mock<IConfigurationRepository>(MockBehavior.Strict);
            var queueItemRepositoryMock = new Mock<IMiddlewareQueueItemRepository>(MockBehavior.Strict);
            var receiptJournalRepositoryMock = new Mock<IMiddlewareReceiptJournalRepository>(MockBehavior.Strict);
            var actionJournalRepositoryMock = new Mock<IMiddlewareActionJournalRepository>(MockBehavior.Strict);
            var journalATRepositoryMock = new Mock<IMiddlewareRepository<ftJournalAT>>(MockBehavior.Strict);
            var journalDERepositoryMock = new Mock<IMiddlewareRepository<ftJournalDE>>(MockBehavior.Strict);
            var journalFRRepositoryMock = new Mock<IMiddlewareJournalFRRepository>(MockBehavior.Strict);
            var journalMERepositoryMock = new Mock<IMiddlewareRepository<ftJournalME>>(MockBehavior.Strict);
            var marketSpecificJournalProcessorMock = new Mock<IMarketSpecificJournalProcessor>(MockBehavior.Strict);
            var queueId = Guid.NewGuid();
            var cashboxId = Guid.NewGuid();
            var receiptRequestMode = 0;
            var receiptHash = "MyHash";

            var queue = new ftQueue
            {
                ftCashBoxId = cashboxId,
                ftQueueId = queueId,
                CountryCode = "AT"
            };

            var queueItem = new ftQueueItem()
            {
                ftQueueItemId = Guid.NewGuid(),
            };
            var request = new JournalRequest()
            {
                ftJournalType = 0x4445000000000000
            };

            var receiptJournal = new ftReceiptJournal()
            {
                ftQueueId = queueId,
                ftQueueItemId = queueItem.ftQueueItemId,
                ftReceiptNumber = queue.ftReceiptNumerator,
                ftReceiptHash = receiptHash
            };

            var configuration = new MiddlewareConfiguration
            {
                QueueId = queueId,
                CashBoxId = cashboxId,
                ReceiptRequestMode = receiptRequestMode,
                Configuration = new() { { "init_ftQueue", JsonConvert.SerializeObject(new List<ftQueue>() { queue }) } }
            };

            marketSpecificJournalProcessorMock.Setup(x => x.ProcessAsync(request)).Returns(new List<JournalResponse>() { new JournalResponse { Chunk = new() } }.ToAsyncEnumerable());
            receiptJournalRepositoryMock.Setup(x => x.InsertAsync(It.IsAny<ftReceiptJournal>())).Returns(Task.CompletedTask);
            configMock.Setup(x => x.InsertOrUpdateQueueAsync(queue)).Returns(Task.CompletedTask);

            var sut = new JournalProcessor(configMock.Object, queueItemRepositoryMock.Object, receiptJournalRepositoryMock.Object, actionJournalRepositoryMock.Object, journalATRepositoryMock.Object, journalDERepositoryMock.Object, journalFRRepositoryMock.Object, journalMERepositoryMock.Object, marketSpecificJournalProcessorMock.Object, loggergMock.Object, configuration);

            var action = () => sut.ProcessAsync(request);

            var e = action.Should().Throw<Exception>().Subject;
            e.First().Message.Should().Contain("The given journal type 0x'4445000000000000' cannot be used with the current Queue, as this export type is not supported in this country.");
        }
    }
}
