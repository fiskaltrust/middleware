using Microsoft.Extensions.Logging;

namespace fiskaltrust.Middleware.Localization.v2.Interface;

public interface IV2QueueBootstrapper
{
    Queue RegisterForSign(ILoggerFactory loggerFactory, IStorageProvider storageProvider);
}