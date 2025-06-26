using System.Threading.Tasks;

namespace fiskaltrust.storage.V0
{
    public interface IQueueItemRepository : IReadOnlyQueueItemRepository
    {
        Task InsertOrUpdateAsync(ftQueueItem queueItem);
    }
}