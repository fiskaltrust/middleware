using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.ifPOS.v1.de;
using fiskaltrust.Middleware.Abstractions;
using fiskaltrust.Middleware.Contracts.Models;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.Middleware.Localization.QueueDE.Helpers;
using fiskaltrust.Middleware.Localization.QueueDE.IntegrationTest.SignProcessorDETests.Fixtures;
using fiskaltrust.Middleware.Localization.QueueDE.IntegrationTest.SignProcessorDETests.Helpers;
using fiskaltrust.Middleware.Localization.QueueDE.MasterData;
using fiskaltrust.Middleware.Localization.QueueDE.Services;
using fiskaltrust.Middleware.Localization.QueueDE.Transactions;
using fiskaltrust.Middleware.Storage.InMemory.Repositories;
using fiskaltrust.Middleware.Storage.InMemory.Repositories.DE;
using fiskaltrust.storage.V0;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Newtonsoft.Json;
using Xunit;

namespace fiskaltrust.Middleware.Localization.QueueDE.IntegrationTest.SignProcessorDETests.ExportModeTests
{
    internal class InMemorySCUWrapper : InMemorySCU
    {
        private readonly bool _isErased;
        public InMemorySCUWrapper(bool isErased)
        {
            _isErased = isErased;
        }

        public override async Task<EndExportSessionResponse> EndExportSessionAsync(EndExportSessionRequest request)
        {
            var response = await base.EndExportSessionAsync(request);
            response.IsErased = _isErased;
            return response;
        }
    }

    public class ExportModeTests : IClassFixture<SignProcessorDependenciesFixture>
    {
        private readonly SignProcessorDependenciesFixture _fixture;

        public ExportModeTests(SignProcessorDependenciesFixture fixture)
        {
            _fixture = fixture;
        }

        [Fact]
        public async Task SignProcessor_ExportModeErasedAndIsErased_ShouldExportToDb()
        {
            var receiptRequest = JsonConvert.DeserializeObject<ReceiptRequest>(File.ReadAllText(Path.Combine("Data", "DailyClosingReceipt", "Request.json")));
            var expectedResponse = JsonConvert.DeserializeObject<ReceiptResponse>(File.ReadAllText(Path.Combine("Data", "DailyClosingReceipt", "Response.json")));
            var queueItem = new ftQueueItem
            {
                cbReceiptMoment = receiptRequest.cbReceiptMoment,
                cbReceiptReference = receiptRequest.cbReceiptReference,
                cbTerminalID = receiptRequest.cbTerminalID,
                country = "DE",
                ftQueueId = Guid.Parse(receiptRequest.ftQueueID),
                ftQueueItemId = Guid.Parse(expectedResponse.ftQueueItemID),
                ftQueueRow = expectedResponse.ftQueueRow,
                request = JsonConvert.SerializeObject(receiptRequest),
                requestHash = "test request hash"
            };
            var queue = new ftQueue
            {
                ftQueueId = Guid.Parse(receiptRequest.ftQueueID),
                StartMoment = DateTime.UtcNow
            };

            await AddOpenOrdersAsync();

            var journalRepositoryMock = new Mock<InMemoryJournalDERepository>()
            {
                CallBase = true
            };
            journalRepositoryMock.Setup(x => x.InsertAsync(It.IsAny<ftJournalDE>())).CallBase();
            var actionJournalRepositoryMock = new Mock<IActionJournalRepository>(MockBehavior.Strict);
            var config = new MiddlewareConfiguration {
                Configuration = new Dictionary<string, object>() {
                    { nameof(QueueDEConfiguration.StoreTemporaryExportFiles), false },
                    { nameof(QueueDEConfiguration.TarFileExportMode), TarFileExportMode.Erased },
                },
                QueueId = queue.ftQueueId,
                ServiceFolder = Path.Combine(Directory.GetCurrentDirectory(), "Test", Guid.NewGuid().ToString())
            };
            var configurationRepository = _fixture.CreateConfigurationRepository();


            var deSSCDProviderMock = new Mock<IDESSCDProvider>();
            deSSCDProviderMock.SetupGet(x => x.Instance).Returns(new InMemorySCUWrapper(true));

            var tarFileCleanupService = new TarFileCleanupService(
                Mock.Of<ILogger<TarFileCleanupService>>(),
                journalRepositoryMock.Object,
                config,
                QueueDEConfiguration.FromMiddlewareConfiguration(Mock.Of<ILogger<QueueDEConfiguration>>(), config)
            );
            var sut = RequestCommandFactoryHelper.ConstructSignProcessor(
                Mock.Of<ILogger<SignProcessorDE>>(),
                configurationRepository,
                journalRepositoryMock.Object,
                actionJournalRepositoryMock.Object,
                deSSCDProviderMock.Object,
                new DSFinVKTransactionPayloadFactory(),
                new InMemoryFailedFinishTransactionRepository(),
                new InMemoryFailedStartTransactionRepository(),
                _fixture.openTransactionRepository,
                Mock.Of<IMasterDataService>(),
                config,
                new InMemoryQueueItemRepository(),
                new SignatureFactoryDE(QueueDEConfiguration.FromMiddlewareConfiguration(Mock.Of<ILogger<QueueDEConfiguration>>(), config)), tarFileCleanupService);


            var (receiptResponse, actionJournals) = await sut.ProcessAsync(receiptRequest, queue, queueItem);

            try
            {
                var journals = await journalRepositoryMock.Object.GetAsync();

                journals.Should().HaveCount(1);
                journals
                    .Where(j => j.ftQueueItemId == Guid.Parse(receiptResponse.ftQueueItemID)).Should().HaveCount(1)
                    .And.Subject.First().Should().Match((ftJournalDE j) => j.Number == 1);
            }
            finally
            {
                if (Directory.Exists(config.ServiceFolder))
                {
                    Directory.Delete(config.ServiceFolder, true);
                }
            }
        }

        [Fact]
        public async Task SignProcessor_ExportModeErasedAndIsNotErased_ShouldNotExportToDb()
        {
            var receiptRequest = JsonConvert.DeserializeObject<ReceiptRequest>(File.ReadAllText(Path.Combine("Data", "DailyClosingReceipt", "Request.json")));
            var expectedResponse = JsonConvert.DeserializeObject<ReceiptResponse>(File.ReadAllText(Path.Combine("Data", "DailyClosingReceipt", "Response.json")));
            var queueItem = new ftQueueItem
            {
                cbReceiptMoment = receiptRequest.cbReceiptMoment,
                cbReceiptReference = receiptRequest.cbReceiptReference,
                cbTerminalID = receiptRequest.cbTerminalID,
                country = "DE",
                ftQueueId = Guid.Parse(receiptRequest.ftQueueID),
                ftQueueItemId = Guid.Parse(expectedResponse.ftQueueItemID),
                ftQueueRow = expectedResponse.ftQueueRow,
                request = JsonConvert.SerializeObject(receiptRequest),
                requestHash = "test request hash"
            };
            var queue = new ftQueue
            {
                ftQueueId = Guid.Parse(receiptRequest.ftQueueID),
                StartMoment = DateTime.UtcNow
            };

            await AddOpenOrdersAsync();

            var journalRepositoryMock = new Mock<InMemoryJournalDERepository>()
            {
                CallBase = true
            };
            journalRepositoryMock.Setup(x => x.InsertAsync(It.IsAny<ftJournalDE>())).CallBase();
            var actionJournalRepositoryMock = new Mock<IActionJournalRepository>(MockBehavior.Strict);
            var config = new MiddlewareConfiguration
            {
                Configuration = new Dictionary<string, object>() {
                    { nameof(QueueDEConfiguration.StoreTemporaryExportFiles), false },
                    { nameof(QueueDEConfiguration.TarFileExportMode), TarFileExportMode.Erased },
                },
                QueueId = queue.ftQueueId,
                ServiceFolder = Path.Combine(Directory.GetCurrentDirectory(), "Test", Guid.NewGuid().ToString())
            };
            var configurationRepository = _fixture.CreateConfigurationRepository();

            var deSSCDProviderMock = new Mock<IDESSCDProvider>();
            deSSCDProviderMock.SetupGet(x => x.Instance).Returns(new InMemorySCUWrapper(false));

            var tarFileCleanupService = new TarFileCleanupService(
                Mock.Of<ILogger<TarFileCleanupService>>(),
                journalRepositoryMock.Object,
                config,
                QueueDEConfiguration.FromMiddlewareConfiguration(Mock.Of<ILogger<QueueDEConfiguration>>(), config)
               );

            var sut = RequestCommandFactoryHelper.ConstructSignProcessor(
                Mock.Of<ILogger<SignProcessorDE>>(),
                configurationRepository,
                journalRepositoryMock.Object,
                actionJournalRepositoryMock.Object,
                deSSCDProviderMock.Object,
                new DSFinVKTransactionPayloadFactory(),
                new InMemoryFailedFinishTransactionRepository(),
                new InMemoryFailedStartTransactionRepository(),
                _fixture.openTransactionRepository,
                Mock.Of<IMasterDataService>(),
                config,
                _fixture.queueItemRepository,
                new SignatureFactoryDE(QueueDEConfiguration.FromMiddlewareConfiguration(Mock.Of<ILogger<QueueDEConfiguration>>(), config)),
                tarFileCleanupService);

            try
            {
                var (receiptResponse, actionJournals) = await sut.ProcessAsync(receiptRequest, queue, queueItem);

                var journals = await journalRepositoryMock.Object.GetAsync();

                journals.Should().HaveCount(0);
            }
            finally
            {
                if (Directory.Exists(config.ServiceFolder))
                {
                    Directory.Delete(config.ServiceFolder, true);
                }
            }
        }

        private async Task AddOpenOrdersAsync()
        {
            await _fixture.AddOpenOrders("A1", 1);
            await _fixture.AddOpenOrders("A2", 2);
            await _fixture.AddOpenOrders("A3", 3);
        }
    }
}
