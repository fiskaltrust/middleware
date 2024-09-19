using fiskaltrust.ifPOS.v1;
using fiskaltrust.Middleware.Contracts.Models;
using Microsoft.Extensions.Logging;

namespace fiskaltrust.Middleware.Localization.v2
{
    public interface IV2QueueBootstrapper
    {
        IPOS CreateQueuePT(MiddlewareConfiguration middlewareConfiguration, ILoggerFactory loggerFactory,IStorageProvider storageProvider);
    }
}