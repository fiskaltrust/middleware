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
    public class AzureOutletMasterDataRepositoryTests : AbstractOutletMasterDataRepositoryTests, IClassFixture<AzureStorageFixture>
    {
        private readonly AzureStorageFixture _fixture;

        public AzureOutletMasterDataRepositoryTests(AzureStorageFixture fixture) => _fixture = fixture;

        public override Task<IMasterDataRepository<OutletMasterData>> CreateReadOnlyRepository(IEnumerable<OutletMasterData> entries) => CreateRepository(entries);

        public override async Task<IMasterDataRepository<OutletMasterData>> CreateRepository(IEnumerable<OutletMasterData> entries)
        {
            var repository = new AzureOutletMasterDataRepository(new QueueConfiguration { QueueId = _fixture.QueueId }, new TableServiceClient(Constants.AzureStorageConnectionString));
            foreach (var entry in entries)
            {
                await repository.InsertAsync(entry);
            }

            return repository;
        }

        public override void DisposeDatabase() => _fixture.CleanTable(nameof(OutletMasterData));
    }
}
