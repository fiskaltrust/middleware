using fiskaltrust.Middleware.Storage.AcceptanceTest;
using fiskaltrust.Middleware.Storage.InMemory.Repositories.AT;
using fiskaltrust.storage.V0;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace fiskaltrust.Middleware.Storage.InMemory.AcceptanceTest
{
    public class InMemoryJournalATRepositoryTests : AbstractJournalATRepositoryTests
    {
        public override Task<IReadOnlyJournalATRepository> CreateReadOnlyRepository(IEnumerable<ftJournalAT> entries) => Task.FromResult<IReadOnlyJournalATRepository>(new InMemoryJournalATRepository(entries));

        public override Task<IJournalATRepository> CreateRepository(IEnumerable<ftJournalAT> entries) => Task.FromResult<IJournalATRepository>(new InMemoryJournalATRepository(entries));
    }
}
