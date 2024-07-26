using System;
using System.Threading.Tasks;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.Interface.Tagging;
using fiskaltrust.Middleware.Contracts.Interfaces;
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
            var request = new ReceiptRequest()
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

            var sut = new SignProcessor(loggergMock.Object, configMock.Object, queueItemRepositoryMock.Object, receiptJournalRepositoryMock.Object, actionJournalRepositoryMock.Object, cryptoHelperMock.Object, marketSpecificSignProcessorMock.Object, configuration, new Mock<ReceiptConverter>().Object);

            await sut.CreateReceiptJournalAsync(queue, queueItem, request);
            receiptJournalRepositoryMock.Verify(x => x.InsertAsync(It.Is<ftReceiptJournal>(rj =>
                rj.ftQueueItemId == queueItem.ftQueueItemId &&
                rj.ftQueueId == queue.ftQueueId &&
                rj.ftReceiptHash == receiptHash &&
                rj.ftReceiptNumber == queue.ftReceiptNumerator
            )), Times.Once());
        }

        [Fact]
        public void ProcessAsync_WhenExceptionIsThrown_Should_ThrowExceptionAndSaveToQueueItem()
        {
            var logger = new Mock<ILogger<SignProcessor>>(MockBehavior.Loose);

            var receiptJournalRepository = new Mock<IReceiptJournalRepository>(MockBehavior.Strict);
            var actionJournalRepository = new Mock<IActionJournalRepository>(MockBehavior.Strict);
            actionJournalRepository.Setup(x => x.InsertAsync(It.IsAny<ftActionJournal>())).Returns(Task.CompletedTask);

            var cryptoHelper = new Mock<ICryptoHelper>(MockBehavior.Strict);
            cryptoHelper.Setup(x => x.GenerateBase64Hash(It.IsAny<string>())).Returns("MyHash");

            var queueId = Guid.NewGuid();
            var cashboxId = Guid.NewGuid();

            var queue = new ftQueue
            {
                ftCashBoxId = cashboxId,
                ftQueueId = queueId,
                ftCurrentRow = 1,
            };


            var configuration = new MiddlewareConfiguration
            {
                QueueId = queueId,
                CashBoxId = cashboxId,
            };

            var configurationRepository = new Mock<IConfigurationRepository>(MockBehavior.Strict);
            configurationRepository.Setup(x => x.GetQueueAsync(queueId)).ReturnsAsync(queue);
            configurationRepository.Setup(x => x.InsertOrUpdateQueueAsync(queue)).Returns(Task.CompletedTask);

            var request = new ReceiptRequest()
            {
                ftCashBoxID = cashboxId.ToString(),
                ftQueueID = queueId.ToString(),
                cbTerminalID = "MyTerminalId",
            };

            var marketSpecificSignProcessor = new Mock<IMarketSpecificSignProcessor>(MockBehavior.Strict);
            marketSpecificSignProcessor.Setup(x => x.ProcessAsync(request, queue, It.IsAny<ftQueueItem>())).ThrowsAsync(new Exception("MyException"));
            marketSpecificSignProcessor.Setup(x => x.GetFtCashBoxIdentificationAsync(queue)).ReturnsAsync("MyCashBoxIdentification");

            var matchResponse = (ftQueueItem queueItem, ReceiptResponse response) =>
            {
                try
                {
                    response.ftCashBoxIdentification.Should().Be("MyCashBoxIdentification");
                    response.ftQueueID.Should().Be(queueId.ToString());
                    response.ftCashBoxID.Should().Be(cashboxId.ToString());
                    response.ftState.Should().Match(x => (x & 0xFFFF_FFFF) == 0xEEEE_EEEE);
                    response.ftQueueItemID.Should().Be(queueItem.ftQueueItemId.ToString());
                    response.cbTerminalID.Should().Be(request.cbTerminalID);
                    response.ftQueueRow.Should().Be(1);
                    response.ftSignatures.Should().HaveCount(1).And.ContainSingle(x =>
                        x.ftSignatureType == ((long) (((ulong) request.ftReceiptCase & 0xFFFF_FFFF_FFFF) | 0x2000_0000_3000))
                        && x.ftSignatureFormat == 0x1
                        && x.Caption == "uncaught-exeption"
                        && x.Data.StartsWith("System.Exception: MyException") && x.Data.Contains("\n")
                    );
                }
                catch
                {
                    return false;
                }

                return true;
            };
            var queueItemRepository = new Mock<IMiddlewareQueueItemRepository>(MockBehavior.Strict);
            queueItemRepository.Setup(x => x.InsertOrUpdateAsync(It.Is<ftQueueItem>(qi => qi.ftQueueId == queueId && qi.response == null))).Returns(Task.CompletedTask).Verifiable();
            queueItemRepository.Setup(x => x.InsertOrUpdateAsync(It.Is<ftQueueItem>(qi => qi.ftQueueId == queueId && qi.response != null && matchResponse(qi, JsonConvert.DeserializeObject<ReceiptResponse>(qi.response))))).Returns(Task.CompletedTask).Verifiable();

            var sut = new SignProcessor(logger.Object, configurationRepository.Object, queueItemRepository.Object, receiptJournalRepository.Object, actionJournalRepository.Object, cryptoHelper.Object, marketSpecificSignProcessor.Object, configuration, new Mock<ReceiptConverter>().Object);

            var process = async () => await sut.ProcessAsync(request);

            process.Should().Throw<Exception>().WithMessage("MyException");
            queueItemRepository.Verify();
        }
    }
}
