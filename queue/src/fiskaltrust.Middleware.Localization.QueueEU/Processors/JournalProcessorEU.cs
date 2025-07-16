using System.IO.Pipelines;
using System.Net.Mime;
using fiskaltrust.ifPOS.v2;
using fiskaltrust.Middleware.Localization.v2;

namespace fiskaltrust.Middleware.Localization.QueueEU.Processors;

public class JournalProcessorEU : IJournalProcessor
{

    public JournalProcessorEU()
    {
    }

    public Task<(ContentType contentType, PipeReader reader)> ProcessAsync(JournalRequest request)
    {
        throw new NotImplementedException();
    }
}
