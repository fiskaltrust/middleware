using System;
using System.Threading.Tasks;
using fiskaltrust.Middleware.Contracts;
using fiskaltrust.Middleware.Contracts.Models;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.storage.V0;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace fiskaltrust.Middleware.Queue.AcceptanceTest
{

    public class SignProcessorUnitTests
    {
        [Fact]
        public async Task CreateReceiptJournalAsync_Should_Match()
        {
            var loggergMock = new Mock<ILogger<SignProcessor>>(MockBehavior.Strict);
            var configMock = new Mock<IConfigurationRepository>(MockBehavior.Strict);
            var queueItemRepositoryMock = new Mock<IMiddlewareQueueItemRepository>(MockBehavior.Strict);
            var receiptJournalRepositoryMock = new Mock<IReceiptJournalRepository>(MockBehavior.Strict);
            var actionJournalRepositoryMock = new Mock<IActionJournalRepository>(MockBehavior.Strict);
            var cryptoHelperMock = new Mock<ICryptoHelper>(MockBehavior.Strict);
            var marketSpecificSignProcessorMock = new Mock<IMarketSpecificSignProcessor>(MockBehavior.Strict);
            var queueId = Guid.NewGuid();
            var cashboxId = Guid.NewGuid();
            var receiptRequestMode = 0;
            var receiptHash = "MyHash";

            var queue = new ftQueue
            {
                ftCashBoxId = cashboxId,
                ftQueueId = queueId
            };

            var queueItem = new ftQueueItem()
            {
                ftQueueItemId = Guid.NewGuid(),
            };
            var request = new ifPOS.v1.ReceiptRequest()
            {
                cbReceiptAmount = 21,
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
                ReceiptRequestMode = receiptRequestMode
            };

            string previousReceiptHash = null;
            cryptoHelperMock.Setup(x => x.GenerateBase64ChainHash(previousReceiptHash, It.Is<ftReceiptJournal>(rj => rj.ftQueueItemId == queueItem.ftQueueItemId), queueItem)).Returns(receiptHash);
            receiptJournalRepositoryMock.Setup(x => x.InsertAsync(It.IsAny<ftReceiptJournal>())).Returns(Task.CompletedTask);
            configMock.Setup(x => x.InsertOrUpdateQueueAsync(queue)).Returns(Task.CompletedTask);

            var sut = new SignProcessor(loggergMock.Object, configMock.Object, queueItemRepositoryMock.Object, receiptJournalRepositoryMock.Object, actionJournalRepositoryMock.Object, cryptoHelperMock.Object, marketSpecificSignProcessorMock.Object, configuration);

            await sut.CreateReceiptJournalAsync(queue, queueItem, request);
            receiptJournalRepositoryMock.Verify(x => x.InsertAsync(It.Is<ftReceiptJournal>(rj =>
                rj.ftQueueItemId == queueItem.ftQueueItemId &&
                rj.ftQueueId == queue.ftQueueId &&
                rj.ftReceiptHash == receiptHash &&
                rj.ftReceiptNumber == queue.ftReceiptNumerator
            )), Times.Once());
        }
    }
}
