using fiskaltrust.ifPOS.v1;
using fiskaltrust.Middleware.Contracts.Interfaces;
using fiskaltrust.Middleware.Contracts.Models;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.storage.V0;
using Microsoft.Extensions.Logging;

namespace fiskaltrust.Middleware.Localization.v2
{
    public interface IV2QueueBootstrapper
    {
        IPOS CreateQueuePT(
                    MiddlewareConfiguration middlewareConfiguration,
                    ILoggerFactory loggerFactory,
                    IConfigurationRepository configurationRepository,
                    IMiddlewareQueueItemRepository middlewareQueueItemRepository,
                    IMiddlewareReceiptJournalRepository middlewareReceiptJournalRepository,
                    IMiddlewareActionJournalRepository middlewareActionJournalRepository,
                    ICryptoHelper cryptoHelper);
    }
}