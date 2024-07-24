using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.Middleware.Storage.AcceptanceTest;
using fiskaltrust.Middleware.Storage.InMemory.Repositories.DE;
using fiskaltrust.storage.V0;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace fiskaltrust.Middleware.Storage.InMemory.AcceptanceTest
{
    public class InMemoryJournalDERepositoryTests : AbstractJournalDERepositoryTests
    {
        public override Task<IReadOnlyJournalDERepository> CreateReadOnlyRepository(IEnumerable<ftJournalDE> entries) => Task.FromResult<IReadOnlyJournalDERepository>(new InMemoryJournalDERepository(entries));

        public override Task<IMiddlewareJournalDERepository> CreateRepository(IEnumerable<ftJournalDE> entries) => Task.FromResult<IMiddlewareJournalDERepository>(new InMemoryJournalDERepository(entries));
    }
}
