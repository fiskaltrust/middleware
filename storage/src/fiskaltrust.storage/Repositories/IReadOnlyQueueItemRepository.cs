using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace fiskaltrust.storage.V0
{
    public interface IReadOnlyQueueItemRepository
    {
        Task<IEnumerable<ftQueueItem>> GetAsync();
        Task<ftQueueItem> GetAsync(Guid id);
    }
}