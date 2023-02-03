using System;
using System.Collections.Generic;
using System.IO;
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

namespace fiskaltrust.Middleware.Localization.QueueDE.IntegrationTest.SignProcessorDETests.TarFileCleanup
{
    public class TarFileCleanupTests : IClassFixture<SignProcessorDependenciesFixture>
    {
        private readonly SignProcessorDependenciesFixture _fixture;

        public TarFileCleanupTests(SignProcessorDependenciesFixture fixture)
        {
            _fixture = fixture;
        }

        [Fact]
        public async Task SignProcessor_TarFileCleanup_ShouldDeleteTarFiles()
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
                    { nameof(QueueDEConfiguration.StoreTemporaryExportFiles), false }
                },
                QueueId = queue.ftQueueId,
                ServiceFolder = Path.Combine(Directory.GetCurrentDirectory(), "Test", Guid.NewGuid().ToString())
            };
            var configurationRepository = _fixture.CreateConfigurationRepository();

            var tarFileCleanupService = new TarFileCleanupService(Mock.Of<ILogger<TarFileCleanupService>>(), journalRepositoryMock.Object, config, QueueDEConfiguration.FromMiddlewareConfiguration(Mock.Of<ILogger<QueueDEConfiguration>>(), config));
            var sut = RequestCommandFactoryHelper.ConstructSignProcessor(Mock.Of<ILogger<SignProcessorDE>>(), configurationRepository, journalRepositoryMock.Object, actionJournalRepositoryMock.Object,
                _fixture.DeSSCDProvider, new DSFinVKTransactionPayloadFactory(), new InMemoryFailedFinishTransactionRepository(), new InMemoryFailedStartTransactionRepository(),
                _fixture.openTransactionRepository, Mock.Of<IMasterDataService>(), config, new InMemoryQueueItemRepository(), new SignatureFactoryDE(QueueDEConfiguration.FromMiddlewareConfiguration(Mock.Of<ILogger<QueueDEConfiguration>>(), config)), tarFileCleanupService);

            try
            {
                var (receiptResponse, actionJournals) = await sut.ProcessAsync(receiptRequest, queue, queueItem);

                Directory.GetFiles(Path.Combine(config.ServiceFolder, "Exports", config.QueueId.ToString(), "TAR")).Should().HaveCount(0);
                Directory.GetDirectories(Path.Combine(config.ServiceFolder, "Exports", config.QueueId.ToString(), "TAR")).Should().HaveCount(0);
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
        public async Task SignProcessor_TarFileCleanup_ShouldDeleteOldTarFiles()
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
                    { nameof(QueueDEConfiguration.StoreTemporaryExportFiles), true }
                },
                QueueId = queue.ftQueueId,
                ServiceFolder = Path.Combine(Directory.GetCurrentDirectory(), "Test", Guid.NewGuid().ToString())
            };
            var configurationRepository = _fixture.CreateConfigurationRepository();

            var tarFileCleanupService = new TarFileCleanupService(Mock.Of<ILogger<TarFileCleanupService>>(), journalRepositoryMock.Object, config, QueueDEConfiguration.FromMiddlewareConfiguration(Mock.Of<ILogger<QueueDEConfiguration>>(), config));
            var sut = RequestCommandFactoryHelper.ConstructSignProcessor(Mock.Of<ILogger<SignProcessorDE>>(), configurationRepository, journalRepositoryMock.Object, actionJournalRepositoryMock.Object,
                _fixture.DeSSCDProvider, new DSFinVKTransactionPayloadFactory(), new InMemoryFailedFinishTransactionRepository(), new InMemoryFailedStartTransactionRepository(),
                _fixture.openTransactionRepository, Mock.Of<IMasterDataService>(), config, new InMemoryQueueItemRepository(), new SignatureFactoryDE(QueueDEConfiguration.FromMiddlewareConfiguration(Mock.Of<ILogger<QueueDEConfiguration>>(), config)), tarFileCleanupService);

            try
            { 
                var (receiptResponse, actionJournals) = await sut.ProcessAsync(receiptRequest, queue, queueItem);

                config.Configuration[nameof(QueueDEConfiguration.StoreTemporaryExportFiles)] = false;

                var bootstrapper = new QueueDEBootstrapper();
                var services = new ServiceCollection();
                services.AddSingleton<IMiddlewareJournalDERepository>(_ => journalRepositoryMock.Object);
                services.AddSingleton<IJournalDERepository>(_ => journalRepositoryMock.Object);
                services.AddSingleton(_ => _fixture.DeSSCDProvider);
                services.AddSingleton(_ => configurationRepository);
                services.AddSingleton(_ => config);
                services.AddSingleton(_ =>
                {
                    var factory = Mock.Of<IClientFactory<IDESSCD>>();
                    Mock.Get(factory).Setup(x => x.CreateClient(It.IsAny<ClientConfiguration>())).Returns(() => _fixture.DeSSCDProvider.Instance);

                    return factory;
                });
                services.AddSingleton(_ => Mock.Of<ILogger<TarFileCleanupService>>());
                services.AddSingleton(_ => Mock.Of<ILogger<QueueDEConfiguration>>());

                bootstrapper.ConfigureServices(services);

                services.BuildServiceProvider().GetRequiredService<ITarFileCleanupService>();

                Directory.GetFiles(Path.Combine(config.ServiceFolder, "Exports", config.QueueId.ToString(), "TAR")).Should().HaveCount(0);
                Directory.GetDirectories(Path.Combine(config.ServiceFolder, "Exports", config.QueueId.ToString(), "TAR")).Should().HaveCount(0);
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
        public async Task SignProcessor_TarFileCleanup_ErasedMode_ShouldDeleteOldTarFiles()
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
                    { nameof(QueueDEConfiguration.StoreTemporaryExportFiles), true },
                    { nameof(QueueDEConfiguration.TarFileExportMode), nameof(TarFileExportMode.Erased).ToLower() } },
                QueueId = queue.ftQueueId,
                ServiceFolder = Path.Combine(Directory.GetCurrentDirectory(), "Test", Guid.NewGuid().ToString())
            };
            var configurationRepository = _fixture.CreateConfigurationRepository();

            var tarFileCleanupService = new TarFileCleanupService(Mock.Of<ILogger<TarFileCleanupService>>(), journalRepositoryMock.Object, config, QueueDEConfiguration.FromMiddlewareConfiguration(Mock.Of<ILogger<QueueDEConfiguration>>(), config));
            var sut = RequestCommandFactoryHelper.ConstructSignProcessor(Mock.Of<ILogger<SignProcessorDE>>(), configurationRepository, journalRepositoryMock.Object, actionJournalRepositoryMock.Object,
                _fixture.DeSSCDProvider, new DSFinVKTransactionPayloadFactory(), new InMemoryFailedFinishTransactionRepository(), new InMemoryFailedStartTransactionRepository(),
                _fixture.openTransactionRepository, Mock.Of<IMasterDataService>(), config, new InMemoryQueueItemRepository(), new SignatureFactoryDE(QueueDEConfiguration.FromMiddlewareConfiguration(Mock.Of<ILogger<QueueDEConfiguration>>(), config)), tarFileCleanupService);

            try
            { 
                var (receiptResponse, actionJournals) = await sut.ProcessAsync(receiptRequest, queue, queueItem);

                config.Configuration[nameof(QueueDEConfiguration.StoreTemporaryExportFiles)] = false;

                var bootstrapper = new QueueDEBootstrapper();
                var services = new ServiceCollection();
                services.AddSingleton<IMiddlewareJournalDERepository>(_ => journalRepositoryMock.Object);
                services.AddSingleton<IJournalDERepository>(_ => journalRepositoryMock.Object);
                services.AddSingleton(_ => _fixture.DeSSCDProvider);
                services.AddSingleton(_ => configurationRepository);
                services.AddSingleton(_ => config);
                services.AddSingleton(_ =>
                {
                    var factory = Mock.Of<IClientFactory<IDESSCD>>();
                    Mock.Get(factory).Setup(x => x.CreateClient(It.IsAny<ClientConfiguration>())).Returns(() => _fixture.DeSSCDProvider.Instance);

                    return factory;
                });
                services.AddSingleton(_ => Mock.Of<ILogger<TarFileCleanupService>>());
                services.AddSingleton(_ => Mock.Of<ILogger<QueueDEConfiguration>>());

                bootstrapper.ConfigureServices(services);

                services.BuildServiceProvider().GetRequiredService<ITarFileCleanupService>();

                Directory.GetFiles(Path.Combine(config.ServiceFolder, "Exports", config.QueueId.ToString(), "TAR")).Should().HaveCount(0);
                Directory.GetDirectories(Path.Combine(config.ServiceFolder, "Exports", config.QueueId.ToString(), "TAR")).Should().HaveCount(0);
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
