using Microsoft.Extensions.Logging;

namespace fiskaltrust.Middleware.Localization.v2.Interface;

public interface IV2QueueBootstrapper
{
    Func<string, Task<string>> RegisterForSign(ILoggerFactory loggerFactory);
}