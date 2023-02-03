using System.Collections.Generic;
using System.Threading.Tasks;
using Azure.Data.Tables;
using fiskaltrust.Middleware.Storage.AcceptanceTest;
using fiskaltrust.Middleware.Storage.AzureTableStorage;
using fiskaltrust.Middleware.Storage.AzureTableStorage.AcceptanceTest.Fixtures;
using fiskaltrust.Middleware.Storage.AzureTableStorage.Repositories;
using fiskaltrust.storage.V0;
using Xunit;

namespace fiskaltrust.Middleware.Storage.AzureTableStorage.AcceptanceTest
{
    [Collection("AzureTableStorageCollection")]
    public class AzureTableStorageConfigurationRepositoryTests : AbstractConfigurationRepositoryTests
    {
        private readonly AzureTableStorageFixture _fixture;

        public AzureTableStorageConfigurationRepositoryTests(AzureTableStorageFixture fixture) => _fixture = fixture;

        public override async Task<IReadOnlyConfigurationRepository> CreateReadOnlyRepository(IEnumerable<ftCashBox> cashBoxes = null, IEnumerable<ftQueue> queues = null, IEnumerable<ftQueueAT> queuesAT = null, IEnumerable<ftQueueDE> queuesDE = null, IEnumerable<ftQueueFR> queuesFR = null, IEnumerable<ftQueueME> queuesME = null, IEnumerable<ftSignaturCreationUnitAT> signatureCreateUnitsAT = null, IEnumerable<ftSignaturCreationUnitDE> signatureCreateUnitsDE = null, IEnumerable<ftSignaturCreationUnitFR> signatureCreateUnitsFR = null, IEnumerable<ftSignaturCreationUnitME> signatureCreateUnitsME = null)
            => await CreateRepository(cashBoxes, queues, queuesAT, queuesDE, queuesFR, queuesME, signatureCreateUnitsAT, signatureCreateUnitsDE, signatureCreateUnitsFR, signatureCreateUnitsME);

        public override async Task<IConfigurationRepository> CreateRepository(IEnumerable<ftCashBox> cashBoxes = null, IEnumerable<ftQueue> queues = null, IEnumerable<ftQueueAT> queuesAT = null, IEnumerable<ftQueueDE> queuesDE = null, IEnumerable<ftQueueFR> queuesFR = null, IEnumerable<ftQueueME> queuesME = null, IEnumerable<ftSignaturCreationUnitAT> signatureCreateUnitsAT = null, IEnumerable<ftSignaturCreationUnitDE> signatureCreateUnitsDE = null, IEnumerable<ftSignaturCreationUnitFR> signatureCreateUnitsFR = null, IEnumerable<ftSignaturCreationUnitME> signatureCreateUnitsME = null)
        {
            var azureConfigurationRepository = new AzureTableStorageConfigurationRepository(new QueueConfiguration { QueueId = _fixture.QueueId }, new TableServiceClient(Constants.AzureStorageConnectionString));

            foreach (var entry in cashBoxes ?? new List<ftCashBox>())
            {
                await azureConfigurationRepository.InsertOrUpdateCashBoxAsync(entry);
            }

            foreach (var entry in queues ?? new List<ftQueue>())
            {
                await azureConfigurationRepository.InsertOrUpdateQueueAsync(entry);
            }

            foreach (var entry in queuesAT ?? new List<ftQueueAT>())
            {
                await azureConfigurationRepository.InsertOrUpdateQueueATAsync(entry);
            }

            foreach (var entry in queuesDE ?? new List<ftQueueDE>())
            {
                await azureConfigurationRepository.InsertOrUpdateQueueDEAsync(entry);
            }

            foreach (var entry in queuesME ?? new List<ftQueueME>())
            {
                await azureConfigurationRepository.InsertOrUpdateQueueMEAsync(entry);
            }

            foreach (var entry in queuesFR ?? new List<ftQueueFR>())
            {
                await azureConfigurationRepository.InsertOrUpdateQueueFRAsync(entry);
            }

            foreach (var entry in signatureCreateUnitsAT ?? new List<ftSignaturCreationUnitAT>())
            {
                await azureConfigurationRepository.InsertOrUpdateSignaturCreationUnitATAsync(entry);
            }

            foreach (var entry in signatureCreateUnitsDE ?? new List<ftSignaturCreationUnitDE>())
            {
                await azureConfigurationRepository.InsertOrUpdateSignaturCreationUnitDEAsync(entry);
            }

            foreach (var entry in signatureCreateUnitsFR ?? new List<ftSignaturCreationUnitFR>())
            {
                await azureConfigurationRepository.InsertOrUpdateSignaturCreationUnitFRAsync(entry);
            }

            foreach (var entry in signatureCreateUnitsME ?? new List<ftSignaturCreationUnitME>())
            {
                await azureConfigurationRepository.InsertOrUpdateSignaturCreationUnitMEAsync(entry);
            }

            return azureConfigurationRepository;
        }

        public override void DisposeDatabase()
        {
            _fixture.CleanTable(nameof(ftCashBox));
            _fixture.CleanTable(nameof(ftQueue));
            _fixture.CleanTable(nameof(ftQueueAT));
            _fixture.CleanTable(nameof(ftQueueDE));
            _fixture.CleanTable(nameof(ftQueueFR));
            _fixture.CleanTable(nameof(ftQueueME));
            _fixture.CleanTable(nameof(ftSignaturCreationUnitAT));
            _fixture.CleanTable(nameof(ftSignaturCreationUnitDE));
            _fixture.CleanTable(nameof(ftSignaturCreationUnitFR));
            _fixture.CleanTable(nameof(ftSignaturCreationUnitME));
        }
    }
}
