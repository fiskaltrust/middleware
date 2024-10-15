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
    public class EFCorePostgreSQLReceiptJournalRepositoryTests : AbstractReceiptJournalRepositoryTests
    {
        private readonly EFCorePostgreSQLStorageCollectionFixture _fixture;

        public EFCorePostgreSQLReceiptJournalRepositoryTests(EFCorePostgreSQLStorageCollectionFixture fixture) => _fixture = fixture;

        public override async Task<IReadOnlyReceiptJournalRepository> CreateReadOnlyRepository(IEnumerable<ftReceiptJournal> entries) => await CreateRepository(entries);

        public override async Task<IMiddlewareReceiptJournalRepository> CreateRepository(IEnumerable<ftReceiptJournal> entries)
        {
            var repository = new EFCoreReceiptJournalRepository(_fixture.Context);
            foreach (var item in entries ?? new List<ftReceiptJournal>())
            {
                await repository.InsertAsync(item);
            }
            return repository;
        }
        public override void DisposeDatabase() => _ = EFCorePostgreSQLStorageCollectionFixture.TruncateTableAsync("ftReceiptJournal");
    }
}
