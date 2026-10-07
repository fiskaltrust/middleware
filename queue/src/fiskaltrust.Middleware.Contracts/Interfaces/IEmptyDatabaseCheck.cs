using System;
using System.Threading.Tasks;

namespace fiskaltrust.Middleware.Contracts.Interfaces
{
    /// <summary>
    /// Runs at queue start and warns if a queue that is already in operation starts with an empty local database.
    /// Storages that start empty by design (e.g. in-memory) register their own no-op implementation.
    /// </summary>
    public interface IEmptyDatabaseCheck
    {
        Task<bool> WarnIfStartedQueueHasEmptyDatabaseAsync(Guid queueId);
    }
}
