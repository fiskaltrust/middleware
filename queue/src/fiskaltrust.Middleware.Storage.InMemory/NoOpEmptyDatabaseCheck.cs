using System;
using System.Threading.Tasks;
using fiskaltrust.Middleware.Contracts.Interfaces;

namespace fiskaltrust.Middleware.Storage.InMemory
{
    /// <summary>
    /// The in-memory storage starts empty on every start by design, so a started queue without data is expected here.
    /// </summary>
    public class NoOpEmptyDatabaseCheck : IEmptyDatabaseCheck
    {
        public Task<bool> WarnIfStartedQueueHasEmptyDatabaseAsync(Guid queueId) => Task.FromResult(false);
    }
}
