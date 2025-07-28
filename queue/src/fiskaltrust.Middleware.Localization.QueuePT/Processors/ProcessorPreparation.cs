using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.Middleware.Localization.v2;
using fiskaltrust.Middleware.Localization.v2.Helpers;

namespace fiskaltrust.Middleware.Localization.QueuePT;

public abstract class ProcessorPreparation
{
    protected abstract AsyncLazy<IMiddlewareQueueItemRepository> _readOnlyQueueItemRepository { get; init; }

    public async Task<T> WithPreparations<T>(ProcessCommandRequest request, Func<Task<T>> process)
    {
        await StaticNumeratorStorage.LoadStorageNumbers(await _readOnlyQueueItemRepository);
        ReceiptRequestValidatorPT.ValidateReceiptOrThrow(request.ReceiptRequest);
        return await process();
    }
}