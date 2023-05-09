using System;
using fiskaltrust.storage.V0;
using System.Threading.Tasks;

namespace fiskaltrust.Middleware.Contracts.Repositories
{
    public interface IQueueRepository
    {
        Task InsertOrUpdateQueueAsync(IQueue queue);

        Task<IQueue> GetQueueAsync(Guid queueId);
    }
}
