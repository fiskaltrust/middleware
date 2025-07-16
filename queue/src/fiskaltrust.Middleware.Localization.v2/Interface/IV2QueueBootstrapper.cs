using System.IO.Pipelines;
using System.Net.Mime;

namespace fiskaltrust.Middleware.Localization.v2.Interface;

public interface IV2QueueBootstrapper
{
    Func<string, Task<string>> RegisterForSign();

    Func<string, Task<string>> RegisterForEcho();

    public Func<string, Task<(ContentType contentType, PipeReader reader)>> RegisterForJournal();
}