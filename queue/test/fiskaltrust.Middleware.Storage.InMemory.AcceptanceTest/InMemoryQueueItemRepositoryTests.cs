using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.Middleware.Storage.AcceptanceTest;
using fiskaltrust.Middleware.Storage.InMemory.Repositories;
using fiskaltrust.storage.V0;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace fiskaltrust.Middleware.Storage.InMemory.AcceptanceTest
{
    public class InMemoryQueueItemRepositoryTests : AbstractQueueItemRepositoryTests
    {
        public override Task<IReadOnlyQueueItemRepository> CreateReadOnlyRepository(IEnumerable<ftQueueItem> entries) => Task.FromResult<IReadOnlyQueueItemRepository>(new InMemoryQueueItemRepository(entries));

        public override async Task<IMiddlewareQueueItemRepository> CreateRepository(IEnumerable<ftQueueItem> entries)
        {
            await SetQueueRowAndTimeStamp(entries.ToList());
            return new InMemoryQueueItemRepository(entries);
        }
    }
}
