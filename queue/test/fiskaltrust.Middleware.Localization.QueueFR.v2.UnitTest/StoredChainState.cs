using fiskaltrust.Middleware.Storage.Base.Helpers;
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
        // A French queue always has its ftQueueFR row (its configuration); the provider refuses to start without one.
        Repository.InsertOrUpdateQueueFRAsync(stored ?? new ftQueueFR { ftQueueFRId = QueueId }).GetAwaiter().GetResult();
    }

    public Guid QueueId { get; }

    public InMemoryConfigurationRepository Repository { get; } = new();

    private FRChainStateProvider? _provider;

    /// <summary>The one provider over this state, as a queue has one.</summary>
    public FRChainStateProvider Provider() => _provider ??= new(new AsyncLazy<IConfigurationRepository>(() => Task.FromResult<IConfigurationRepository>(Repository)), QueueId);

    /// <summary>What FinishQueueItem does once the signed queue item is stored.</summary>
    public Task CommitAsync() => Provider().CommitAsync();

    public Task<ftQueueFR> ReadAsync() => Repository.GetQueueFRAsync(QueueId);
}
