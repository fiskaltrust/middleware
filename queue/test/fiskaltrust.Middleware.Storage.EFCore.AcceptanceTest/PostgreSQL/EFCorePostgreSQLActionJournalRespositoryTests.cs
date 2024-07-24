using System.Collections.Generic;
using System.Threading.Tasks;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.Middleware.Storage.AcceptanceTest;
using fiskaltrust.Middleware.Storage.EFCore.AcceptanceTest.PostgreSQL.Fixtures;
using fiskaltrust.Middleware.Storage.EFCore.Repositories;
using fiskaltrust.storage.V0;
using Xunit;

namespace fiskaltrust.Middleware.Storage.EFCore.AcceptanceTest.PostgreSQL
{
    [Collection(EFCorePostgreSQLStorageCollectionFixture.CollectionName)]
    public class EFCorePostgreSQLActionJournalRespositoryTests : AbstractActionJournalRepositoryTests
    {
        private readonly EFCorePostgreSQLStorageCollectionFixture _fixture;

        public EFCorePostgreSQLActionJournalRespositoryTests(EFCorePostgreSQLStorageCollectionFixture fixture) => _fixture = fixture;

        public override async Task<IReadOnlyActionJournalRepository> CreateReadOnlyRepository(IEnumerable<ftActionJournal> entries) => await CreateRepository(entries);

        public override async Task<IMiddlewareActionJournalRepository> CreateRepository(IEnumerable<ftActionJournal> entries)
        {
            var repository = new EFCoreActionJournalRepository(_fixture.Context);
            foreach (var item in entries ?? new List<ftActionJournal>())
            {
                await repository.InsertAsync(item);
            }
            return repository;
        }
        public override void DisposeDatabase() => _ = EFCorePostgreSQLStorageCollectionFixture.TruncateTableAsync("ftActionJournal");
    }
}
