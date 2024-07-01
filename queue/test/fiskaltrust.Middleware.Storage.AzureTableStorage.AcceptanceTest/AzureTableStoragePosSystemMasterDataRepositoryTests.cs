using System.Collections.Generic;
using System.Threading.Tasks;
using Azure.Data.Tables;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.Middleware.Storage.AcceptanceTest;
using fiskaltrust.Middleware.Storage.AzureTableStorage;
using fiskaltrust.Middleware.Storage.AzureTableStorage.AcceptanceTest.Fixtures;
using fiskaltrust.Middleware.Storage.AzureTableStorage.Repositories.MasterData;
using fiskaltrust.storage.V0.MasterData;
using Xunit;

namespace fiskaltrust.Middleware.Storage.AzureTableStorage.AcceptanceTest
{
    [Collection("AzureTableStorageCollection")]
    public class AzureTableStoragePosSystemMasterDataRepositoryTests : AbstractPosSystemMasterDataRepositoryTests
    {
        private readonly AzureTableStorageFixture _fixture;

        public AzureTableStoragePosSystemMasterDataRepositoryTests(AzureTableStorageFixture fixture) => _fixture = fixture;

        public override Task<IMasterDataRepository<PosSystemMasterData>> CreateReadOnlyRepository(IEnumerable<PosSystemMasterData> entries) => CreateRepository(entries);

        public override async Task<IMasterDataRepository<PosSystemMasterData>> CreateRepository(IEnumerable<PosSystemMasterData> entries)
        {
            var repository = new AzureTableStoragePosSystemMasterDataRepository(new QueueConfiguration { QueueId = _fixture.QueueId }, new TableServiceClient(Constants.AzureStorageConnectionString));
            foreach (var entry in entries)
            {
                await repository.InsertAsync(entry);
            }

            return repository;
        }

        public override void DisposeDatabase() => _fixture.CleanTable(AzureTableStoragePosSystemMasterDataRepository.TABLE_NAME);
    }
}
