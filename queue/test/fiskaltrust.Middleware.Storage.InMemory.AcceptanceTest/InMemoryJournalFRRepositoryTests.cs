using fiskaltrust.Middleware.Storage.AcceptanceTest;
using fiskaltrust.Middleware.Storage.InMemory.Repositories.FR;
using fiskaltrust.storage.V0;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace fiskaltrust.Middleware.Storage.InMemory.AcceptanceTest
{
    public class InMemoryJournalFRRepositoryTests : AbstractJournalFRRepositoryTests
    {
        public override Task<IReadOnlyJournalFRRepository> CreateReadOnlyRepository(IEnumerable<ftJournalFR> entries) => Task.FromResult<IReadOnlyJournalFRRepository>(new InMemoryJournalFRRepository(entries));

        public override Task<IJournalFRRepository> CreateRepository(IEnumerable<ftJournalFR> entries) => Task.FromResult<IJournalFRRepository>(new InMemoryJournalFRRepository(entries));
    }
}
