using fiskaltrust.Middleware.Storage.AcceptanceTest;
using fiskaltrust.Middleware.Storage.InMemory.Repositories;
using fiskaltrust.storage.V0;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace fiskaltrust.Middleware.Storage.InMemory.AcceptanceTest
{
    public class InMemoryReceiptJournalRepositoryTests : AbstractReceiptJournalRepositoryTests
    {
        public override Task<IReadOnlyReceiptJournalRepository> CreateReadOnlyRepository(IEnumerable<ftReceiptJournal> entries) => Task.FromResult<IReadOnlyReceiptJournalRepository>(new InMemoryReceiptJournalRepository(entries));

        public override Task<IReceiptJournalRepository> CreateRepository(IEnumerable<ftReceiptJournal> entries) => Task.FromResult<IReceiptJournalRepository>(new InMemoryReceiptJournalRepository(entries));
    }
}
