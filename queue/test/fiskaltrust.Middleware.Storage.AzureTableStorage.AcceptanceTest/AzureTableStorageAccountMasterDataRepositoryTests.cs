using System.Collections.Generic;
using System.Threading.Tasks;
using Azure.Data.Tables;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.Middleware.Storage.AcceptanceTest;
using fiskaltrust.Middleware.Storage.AzureTableStorage.AcceptanceTest.Fixtures;
using fiskaltrust.Middleware.Storage.AzureTableStorage.Repositories.MasterData;
using fiskaltrust.storage.V0.MasterData;
using Xunit;

namespace fiskaltrust.Middleware.Storage.AzureTableStorage.AcceptanceTest
{
    [Collection("AzureTableStorageCollection")]
    public class AzureTableStorageAccountMasterDataRepositoryTests: AbstractAccountMasterDataRepositoryTests
    {
        private readonly AzureTableStorageFixture _fixture;

        public AzureTableStorageAccountMasterDataRepositoryTests(AzureTableStorageFixture fixture) => _fixture = fixture;

        public override Task<IMasterDataRepository<AccountMasterData>> CreateReadOnlyRepository(IEnumerable<AccountMasterData> entries) => CreateRepository(entries);

        public override async Task<IMasterDataRepository<AccountMasterData>> CreateRepository(IEnumerable<AccountMasterData> entries)
        {
            var repository = new AzureTableStorageAccountMasterDataRepository(new QueueConfiguration { QueueId = _fixture.QueueId }, new TableServiceClient(Constants.AzureStorageConnectionString));
            foreach (var entry in entries)
            {
                await repository.InsertAsync(entry).ConfigureAwait(false);
            }

            return repository;
        }

        public override void DisposeDatabase() => _fixture.CleanTable(AzureTableStorageAccountMasterDataRepository.TABLE_NAME);
    }
}
