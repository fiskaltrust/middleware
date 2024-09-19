﻿using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.storage.V0;

namespace fiskaltrust.Middleware.Localization.v2
{
    public interface IStorageProvider
    {
        IConfigurationRepository GetConfigurationRepository();
        IMiddlewareQueueItemRepository GetMiddlewareQueueItemRepository();
        IMiddlewareReceiptJournalRepository GetMiddlewareReceiptJournalRepository();
        IMiddlewareActionJournalRepository GetMiddlewareActionJournalRepository();
    }
}