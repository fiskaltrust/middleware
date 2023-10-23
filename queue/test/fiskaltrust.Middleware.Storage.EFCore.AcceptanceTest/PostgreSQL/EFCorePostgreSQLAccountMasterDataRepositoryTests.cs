using System.Collections.Generic;
using System.Threading.Tasks;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.Middleware.Storage.AcceptanceTest;
using fiskaltrust.Middleware.Storage.EFCore.AcceptanceTest.PostgreSQL.Fixtures;
using fiskaltrust.Middleware.Storage.EFCore.Repositories.MasterData;
using fiskaltrust.storage.V0.MasterData;
using Xunit;

namespace fiskaltrust.Middleware.Storage.EFCore.AcceptanceTest.PostgreSQL
{
    [Collection(EFCorePostgreSQLStorageCollectionFixture.CollectionName)]
    public class EFCorePostgreSQLAccountMasterDataRepositoryTests : AbstractAccountMasterDataRepositoryTests
    {
        private readonly EFCorePostgreSQLStorageCollectionFixture _fixture;

        public EFCorePostgreSQLAccountMasterDataRepositoryTests(EFCorePostgreSQLStorageCollectionFixture fixture) => _fixture = fixture;

        public override Task<IMasterDataRepository<AccountMasterData>> CreateReadOnlyRepository(IEnumerable<AccountMasterData> entries) => CreateRepository(entries);

        public override async Task<IMasterDataRepository<AccountMasterData>> CreateRepository(IEnumerable<AccountMasterData> entries)
        {
            var repository = new EFCoreAccountMasterDataRepository(_fixture.Context);
            foreach (var item in entries ?? new List<AccountMasterData>())
            {
                await repository.InsertAsync(item);
            }

            return repository;
        }

        public override void DisposeDatabase() => _= EFCorePostgreSQLStorageCollectionFixture.TruncateTableAsync("AccountMasterData");
    }
}
