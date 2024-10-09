using fiskaltrust.Api.POS.Models.ifPOS.v2;
using fiskaltrust.Middleware.Contracts.Models;
using Microsoft.Extensions.Logging;

namespace fiskaltrust.Middleware.Localization.v2;

public interface IV2QueueBootstrapper
{
    Func<string, Task<string>> RegisterForSign(MiddlewareConfiguration middlewareConfiguration, ILoggerFactory loggerFactory, IStorageProvider storageProvider)
}