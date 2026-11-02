using fiskaltrust.Middleware.Localization.QueueFR.v2.Logic;
using fiskaltrust.Middleware.Localization.v2.Helpers;
using fiskaltrust.Middleware.Storage.InMemory.Repositories;
using fiskaltrust.storage.V0;

namespace fiskaltrust.Middleware.Localization.QueueFR.v2.UnitTest;

/// <summary>An in-memory ftQueueFR the chain state is read from and written to (market-fr ADR 0012).</summary>
internal sealed class StoredChainState
{
    public StoredChainState(ftQueueFR? stored = null)
    {
        QueueId = stored?.ftQueueFRId ?? Guid.NewGuid();
        if (stored is not null)
        {
            Repository.InsertOrUpdateQueueFRAsync(stored).GetAwaiter().GetResult();
        }
    }

    public Guid QueueId { get; }

    public InMemoryConfigurationRepository Repository { get; } = new();

    public FRChainStateProvider Provider() => new(new AsyncLazy<IConfigurationRepository>(() => Task.FromResult<IConfigurationRepository>(Repository)), QueueId);

    public Task<ftQueueFR> ReadAsync() => Repository.GetQueueFRAsync(QueueId);
}
