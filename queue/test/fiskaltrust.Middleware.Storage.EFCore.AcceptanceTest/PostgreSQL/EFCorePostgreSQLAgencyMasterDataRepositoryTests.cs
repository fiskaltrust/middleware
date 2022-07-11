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
    public class EFCorePostgreSQLAgencyMasterDataRepositoryTests : AbstractAgencyMasterDataRepositoryTests
    {
        private readonly EFCorePostgreSQLStorageCollectionFixture _fixture;

        public EFCorePostgreSQLAgencyMasterDataRepositoryTests(EFCorePostgreSQLStorageCollectionFixture fixture) => _fixture = fixture;

        public override Task<IMasterDataRepository<AgencyMasterData>> CreateReadOnlyRepository(IEnumerable<AgencyMasterData> entries) => CreateRepository(entries);

        public override async Task<IMasterDataRepository<AgencyMasterData>> CreateRepository(IEnumerable<AgencyMasterData> entries)
        {
            var repository = new EFCoreAgencyMasterDataRepository(_fixture.Context);
            foreach (var item in entries ?? new List<AgencyMasterData>())
            {
                await repository.InsertAsync(item);
            }
            return repository;
        }
        public override void DisposeDatabase() => _ = EFCorePostgreSQLStorageCollectionFixture.TruncateTableAsync("AgencyMasterData");
    }
}
