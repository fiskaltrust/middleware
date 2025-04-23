using System.Collections.Generic;
using System.Threading.Tasks;
using Azure.Data.Tables;
using fiskaltrust.Middleware.Storage.AcceptanceTest;
using fiskaltrust.Middleware.Storage.AzureTableStorage;
using fiskaltrust.Middleware.Storage.AzureTableStorage.AcceptanceTest.Fixtures;
using fiskaltrust.Middleware.Storage.AzureTableStorage.Repositories;
using fiskaltrust.Middleware.Storage.AzureTableStorage.Repositories.Configuration;
using fiskaltrust.storage.V0;
using Xunit;

namespace fiskaltrust.Middleware.Storage.AzureTableStorage.AcceptanceTest
{
    [Collection("AzureTableStorageCollection")]
    public class AzureTableStorageConfigurationRepositoryTests : AbstractConfigurationRepositoryTests
    {
        private readonly AzureTableStorageFixture _fixture;

        public AzureTableStorageConfigurationRepositoryTests(AzureTableStorageFixture fixture) => _fixture = fixture;

        public override async Task<storage.V0.IReadOnlyConfigurationRepository> CreateReadOnlyRepository(IEnumerable<ftCashBox> cashBoxes = null, IEnumerable<ftQueue> queues = null, IEnumerable<ftQueueAT> queuesAT = null, IEnumerable<ftQueueDE> queuesDE = null, IEnumerable<ftQueueFR> queuesFR = null, IEnumerable<ftQueueIT> queuesIT = null, IEnumerable<ftQueueME> queuesME = null, IEnumerable<ftSignaturCreationUnitAT> signatureCreateUnitsAT = null, IEnumerable<ftSignaturCreationUnitDE> signatureCreateUnitsDE = null, IEnumerable<ftSignaturCreationUnitFR> signatureCreateUnitsFR = null, IEnumerable<ftSignaturCreationUnitIT> signatureCreateUnitsIT = null, IEnumerable<ftSignaturCreationUnitME> signatureCreateUnitsME = null)
            => await CreateRepository(cashBoxes, queues, queuesAT, queuesDE, queuesFR, queuesIT, queuesME, signatureCreateUnitsAT, signatureCreateUnitsDE, signatureCreateUnitsFR, signatureCreateUnitsIT, signatureCreateUnitsME);

        public override async Task<storage.V0.IConfigurationRepository> CreateRepository(IEnumerable<ftCashBox> cashBoxes = null, IEnumerable<ftQueue> queues = null, IEnumerable<ftQueueAT> queuesAT = null, IEnumerable<ftQueueDE> queuesDE = null, IEnumerable<ftQueueFR> queuesFR = null, IEnumerable<ftQueueIT> queuesIT = null, IEnumerable<ftQueueME> queuesME = null, IEnumerable<ftSignaturCreationUnitAT> signatureCreateUnitsAT = null, IEnumerable<ftSignaturCreationUnitDE> signatureCreateUnitsDE = null, IEnumerable<ftSignaturCreationUnitFR> signatureCreateUnitsFR = null, IEnumerable<ftSignaturCreationUnitIT> signatureCreateUnitsIT = null, IEnumerable<ftSignaturCreationUnitME> signatureCreateUnitsME = null)
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

            foreach (var entry in queuesIT ?? new List<ftQueueIT>())
            {
                await azureConfigurationRepository.InsertOrUpdateQueueITAsync(entry);
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

            foreach (var entry in signatureCreateUnitsIT ?? new List<ftSignaturCreationUnitIT>())
            {
                await azureConfigurationRepository.InsertOrUpdateSignaturCreationUnitITAsync(entry);
            }

            foreach (var entry in signatureCreateUnitsME ?? new List<ftSignaturCreationUnitME>())
            {
                await azureConfigurationRepository.InsertOrUpdateSignaturCreationUnitMEAsync(entry);
            }

            return azureConfigurationRepository;
        }

        public override void DisposeDatabase()
        {
            _fixture.CleanTable(AzureTableStorageCashBoxRepository.TABLE_NAME);
            _fixture.CleanTable(AzureTableStorageQueueRepository.TABLE_NAME);
            _fixture.CleanTable(AzureTableStorageQueueATRepository.TABLE_NAME);
            _fixture.CleanTable(AzureTableStorageQueueDERepository.TABLE_NAME);
            _fixture.CleanTable(AzureTableStorageQueueFRRepository.TABLE_NAME);
            _fixture.CleanTable(AzureTableStorageQueueITRepository.TABLE_NAME);
            _fixture.CleanTable(AzureTableStorageQueueMERepository.TABLE_NAME);
            _fixture.CleanTable(AzureTableStorageSignaturCreationUnitATRepository.TABLE_NAME);
            _fixture.CleanTable(AzureTableStorageSignaturCreationUnitDERepository.TABLE_NAME);
            _fixture.CleanTable(AzureTableStorageSignaturCreationUnitFRRepository.TABLE_NAME);
            _fixture.CleanTable(AzureTableStorageSignaturCreationUnitITRepository.TABLE_NAME);
            _fixture.CleanTable(AzureTableStorageSignaturCreationUnitMERepository.TABLE_NAME);
        }
    }
}
