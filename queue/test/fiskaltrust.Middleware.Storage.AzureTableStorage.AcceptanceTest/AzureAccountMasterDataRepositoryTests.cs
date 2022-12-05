using System;
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
    public class AzureAccountMasterDataRepositoryTests : AbstractAccountMasterDataRepositoryTests, IClassFixture<AzureStorageFixture>
    {
        private readonly AzureStorageFixture _fixture;

        public AzureAccountMasterDataRepositoryTests(AzureStorageFixture fixture) => _fixture = fixture;

        public override Task<IMasterDataRepository<AccountMasterData>> CreateReadOnlyRepository(IEnumerable<AccountMasterData> entries) => CreateRepository(entries);

        public override async Task<IMasterDataRepository<AccountMasterData>> CreateRepository(IEnumerable<AccountMasterData> entries)
        {
            var repository = new AzureAccountMasterDataRepository(new QueueConfiguration { QueueId = _fixture.QueueId }, new TableServiceClient(Constants.AzureStorageConnectionString));
            foreach (var entry in entries)
            {
                await repository.InsertAsync(entry).ConfigureAwait(false);
            }

            return repository;
        }

        public override void DisposeDatabase() => _fixture.CleanTable(nameof(AccountMasterData));
    }
}
