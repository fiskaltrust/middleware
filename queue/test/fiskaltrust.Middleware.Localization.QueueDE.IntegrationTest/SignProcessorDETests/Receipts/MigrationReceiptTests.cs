using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.Middleware.Contracts.Models;
using fiskaltrust.Middleware.Localization.QueueDE.IntegrationTest.SignProcessorDETests.Fixtures;
using fiskaltrust.Middleware.Localization.QueueDE.IntegrationTest.SignProcessorDETests.Helpers;
using fiskaltrust.Middleware.Localization.QueueDE.MasterData;
using fiskaltrust.Middleware.Localization.QueueDE.Transactions;
using fiskaltrust.Middleware.Storage.InMemory.Repositories;
using fiskaltrust.Middleware.Storage.InMemory.Repositories.DE;
using fiskaltrust.Middleware.Localization.QueueDE.Helpers;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using fiskaltrust.Middleware.Queue;
using Xunit;
using fiskaltrust.Middleware.Contracts.Interfaces;
using Newtonsoft.Json;
using fiskaltrust.Middleware.Localization.QueueDE.Models;
using System.Linq;

namespace fiskaltrust.Middleware.Localization.QueueDE.IntegrationTest.SignProcessorDETests.Receipts
{
    public class MigrationReceiptTests
    {
        private readonly SignProcessorDependenciesFixture _fixture;
        public MigrationReceiptTests() => _fixture = new SignProcessorDependenciesFixture();
        [Fact]
        public async Task SignProcessor_MigrationScript_ShouldSaveInfoBlockFurtherCalls()
        {
            var configRepo = _fixture.CreateConfigurationRepository(false, DateTime.Now.AddDays(-100));
            var queue = await configRepo.GetQueueAsync(_fixture.QUEUEID).ConfigureAwait(false);

            var request = new ReceiptRequest
            {
                ftQueueID = queue.ftQueueId.ToString(),
                cbReceiptMoment = DateTime.UtcNow,
                cbTerminalID = "test terminal",
                cbReceiptReference = "Migration",
                ftReceiptCase = 0x4445000100000019,
                ftCashBoxID = _fixture.CASHBOXID.ToString()
            };
            var journalRepository = new InMemoryJournalDERepository();
            var actionJournalRepository = new InMemoryActionJournalRepository();
            var queueItemRepository = new InMemoryQueueItemRepository();
            var receiptJournalRepository = new InMemoryReceiptJournalRepository();

            var config = new MiddlewareConfiguration
            {
                Configuration = new Dictionary<string, object>(),
                CashBoxId = _fixture.CASHBOXID,
                QueueId = _fixture.QUEUEID,
                ProcessingVersion = "test"
            };
            var signProcessorDE = RequestCommandFactoryHelper.ConstructSignProcessor(Mock.Of<ILogger<SignProcessorDE>>(), configRepo, journalRepository,
                actionJournalRepository, _fixture.DeSSCDProvider, new DSFinVKTransactionPayloadFactory(Mock.Of<ILogger<DSFinVKTransactionPayloadFactory>>()), new InMemoryFailedFinishTransactionRepository(),
                new InMemoryFailedStartTransactionRepository(), new InMemoryOpenTransactionRepository(), Mock.Of<IMasterDataService>(), config,
                queueItemRepository, new SignatureFactoryDE(QueueDEConfiguration.FromMiddlewareConfiguration(Mock.Of<ILogger<QueueDEConfiguration>>(), config)));

            var signProcessor = new SignProcessor(Mock.Of<ILogger<SignProcessor>>(), configRepo, queueItemRepository, receiptJournalRepository,
                             actionJournalRepository, Mock.Of<ICryptoHelper>(), signProcessorDE, config);

            var receiptResponse = await signProcessor.ProcessAsync(request);

            var queueItem = await queueItemRepository.GetLastQueueItemAsync().ConfigureAwait(false);
            receiptResponse.ftQueueItemID.Should().Be(queueItem.ftQueueItemId.ToString());

            var actionjournala = await actionJournalRepository.GetAsync().ConfigureAwait(false);
            var actionjournal = actionjournala.Last();

            var migrationState = JsonConvert.DeserializeObject<MigrationState>(actionjournal.DataJson);
            var ajCount = await actionJournalRepository.CountAsync().ConfigureAwait(false);
            migrationState.ActionJournalCount.Should().Be(ajCount - 1);
            var qCount = await queueItemRepository.CountAsync().ConfigureAwait(false);
            migrationState.QueueItemCount = qCount;
            var rjCount = await receiptJournalRepository.CountAsync().ConfigureAwait(false);
            migrationState.ReceiptJournalCount = rjCount;
            var jdCount = await journalRepository.CountAsync().ConfigureAwait(false);
            migrationState.JournalDECount = jdCount;
            migrationState.QueueRow.Should().Be(queue.ftQueuedRow);

            request = new ReceiptRequest
            {
                ftQueueID = queue.ftQueueId.ToString(),
                cbReceiptMoment = DateTime.UtcNow,
                cbTerminalID = "test terminal",
                cbReceiptReference = "pos",
                ftReceiptCase = 0x4445000100000000,
                ftCashBoxID = _fixture.CASHBOXID.ToString()
            };

            var sutMethod = CallSignProcessor_ExpectException(signProcessor, request);
            await sutMethod.Should().ThrowAsync<InvalidOperationException>().WithMessage(MigrationHelper.ExceptionMessage).ConfigureAwait(false);

        }

        private Func<Task> CallSignProcessor_ExpectException(SignProcessor signProcessor, ReceiptRequest receiptRequest)
        {
            return async () => { var receiptResponse = await signProcessor.ProcessAsync(receiptRequest); };
        }
    }
}
