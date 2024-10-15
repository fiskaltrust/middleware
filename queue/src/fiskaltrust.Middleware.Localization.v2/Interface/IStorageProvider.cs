using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.storage.V0;

namespace fiskaltrust.Middleware.Localization.v2.Interface;

public interface IStorageProvider
{
    Task Initialized { get; }
    IConfigurationRepository GetConfigurationRepository();
    IMiddlewareQueueItemRepository GetMiddlewareQueueItemRepository();
    IMiddlewareReceiptJournalRepository GetMiddlewareReceiptJournalRepository();
    IMiddlewareActionJournalRepository GetMiddlewareActionJournalRepository();
}