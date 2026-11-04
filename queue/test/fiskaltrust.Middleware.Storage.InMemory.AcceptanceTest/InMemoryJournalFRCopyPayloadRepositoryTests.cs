using System.Collections.Generic;
using System.Threading.Tasks;
using fiskaltrust.Middleware.Contracts.Models.FR;
using fiskaltrust.Middleware.Contracts.Repositories.FR;
using fiskaltrust.Middleware.Storage.AcceptanceTest;
using fiskaltrust.Middleware.Storage.InMemory.Repositories.FR;

namespace fiskaltrust.Middleware.Storage.InMemory.AcceptanceTest
{
    public class InMemoryJournalFRCopyPayloadRepositoryTests : AbstractJournalFRCopyPayloadRepositoryTests
    {
        public override Task<IJournalFRCopyPayloadRepository> CreateRepository(IEnumerable<ftJournalFRCopyPayload> entries)
            => Task.FromResult<IJournalFRCopyPayloadRepository>(new InMemoryJournalFRCopyPayloadRepository(entries));
    }
}
